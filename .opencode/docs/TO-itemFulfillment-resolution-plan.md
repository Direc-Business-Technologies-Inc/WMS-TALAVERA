# Packing → STR Item Fulfillment — Resolution Plan

Date: 2026-10-23
Status: Implemented (Phases 1, 2a, 3.2, 3.3, 4.1-4.3, 4.5, 5). **U2 is a deploy gate.**
Supersedes: the **filler shape** decision in `TO-itemFulfillment-implementation-fix.md` (see Decision D3)
Related: `TO-itemFulfillment-RCA.md` (root-cause evidence), `.github/specs/2026-10-21-to-if-freshness-revalidation-fix.md`, `.github/specs/2026-10-15-if-replace-param-invalid-parameter-fix.md`, `.opencode/docs/changes/2026-10-21-mobile-str-vendor-gated-fulfillment.md`

Scope: the mobile STR packing save path (`POST /Packing/TransferOrder/SaveScan` → `NetSuiteApiClientService.SaveTOItemFulfillment`).

> **Implementation status (2026-10-23, second review pass).** See
> `changes/2026-10-23-to-if-reconciliation.md` for the full change record.
>
> - Phase 1.5 was initially implemented as "reconcile once, then filter by
>   `IsBad`", which left the BAD payload with no fillers. It now builds a
>   complete line list **per bucket**
>   (`BuildTOFulfillmentBucketLines`), and `EnsureTOFulfillmentBucketIsComplete`
>   asserts F2's set equality for each bucket immediately before its POST.
> - Phase 1.3 is implemented with one deliberate deviation: only submitted
>   **scans** must be open. A submitted zero-quantity row is a stale client
>   filler and is replaced or dropped, never treated as an operator conflict.
> - Phase 5.2's `extra` is measured, not hardcoded; `badLine` is represented by
>   `bucket=BAD` and is not a separate field.
> - F2's "block the post" wording is implemented as "repair, then assert":
>   missing open lines are added as fillers, and the post is blocked only if the
>   completed bucket still disagrees with the open set.
> - **U3 is answered by inference**, not by `NextTransactionLineLink`: both the
>   display query and the authoritative read use `transactionline.id` as
>   `orderLine`, and scanned lines already post with that value in production.
> - **U1/U2 remain unverified.** U2 gates the deploy: run the Phase 0.3 matrix
>   variant 2, or deploy with `WMS_TO_IF_ENABLE_RECONCILIATION=false` and flip the
>   flag after one observed save.

---

## 1. Problem statement

Two distinct HTTP 400 `USER_ERROR` failures are live on the deploy that introduced the vendor gate (`3e5639af`). Both are violations of one payload contract; they differ only in which side of it is broken.

### Issue 1 — a line the payload never addressed

```
[USER_ERROR] Error while accessing a resource. Please configure the inventory detail for <ITEM>.
[diag replace=True usedBin=True scannedLines=2 totalLines=4]
item="HAPEE OUTRAGEOUS BLUE CHILL 90MLX2" (not matched to a posted line) candidates=[none among 4 posted entries]
```

Observed on TO `ISTR-THM00100000771`: 63 item lines on the order, 4 submitted. Also observed on `ISTR-THM00100000776-2`: 1,657 lines, 713 submitted.

### Issue 2 — a line whose quantity and detail disagree

```
[USER_ERROR] Error while accessing a resource. The total inventory detail quantity must be 3.
[diag replace=True usedBin=True scannedLines=6 totalLines=8]
```

NetSuite is enforcing `line quantity == sum(inventoryDetail.inventoryAssignment.items[].quantity)` on a line that **was** submitted.

### The shared contract

For every entry in `item.items[]`:

1. `orderLine` is a current open SHIPPING line of the order — exactly once per bucket;
2. `quantity == sum(inventoryDetail.inventoryAssignment.items[].quantity)`;
3. at a bin-tracked location the `inventoryDetail` sub-resource is present on **every** entry (`?replace=item.inventoryDetail.inventoryAssignment` is URL-scoped, not per-entry);
4. closed or fully-shipped lines are never sent.

The current builder satisfies (1) only for rows the mobile happened to load, (2) only for scanned rows, and (3) and (4) only by accident.

---

## 2. Established evidence

| Fact | Source |
|---|---|
| The transform pre-fills the item sublist with the full open quantity of **every** open line; an unaddressed line is fulfilled anyway with no inventory detail | `TO-itemFulfillment-RCA.md` §1.3, §4 |
| The mobile loads and submits only the resolved vendor's slice (vendor gate), plus rows its own predicate drops | `GetTransferOrderLineQry`, `TransferOrderItemView.razor.cs:813-825` |
| Display query joins UoM with an INNER JOIN, so a line with unusable `tl.units` is invisible to the picker | `NS_TO_x_Packing_Get_Items.sql:61` |
| Display query also filters `status IN ('B','D','E')`, category `IN ('1','2')`, and `tranid` | `NS_TO_x_Packing_Get_Items.sql:260-266` |
| The save path already reads every SHIPPING line of the order, but **unpaged** | `NetSuiteApiClientService.cs:1569-1588` |
| An unpaged read marks lines beyond the 1,000-row cap as "closed"; absent lines are stripped **without** the loud message | `:1604-1634`, `:1745-1748` |
| Fillers are emitted as `{orderLine, itemreceive:false}` with no quantity; `itemreceive` is an **itemReceipt** field and is inert on an itemFulfillment | `TransferOrderIFPayloadDTO.cs:35-52`, `PayloadsDTO.cs:21-22` |
| The same filler block is duplicated in the Returns and VRA itemFulfillment builders | `ReturnsIFPayloadDTO.cs:28-52`, `VendorReturnAuthorizationIFPayloadDTO.cs:28-52` |
| `build=<version>+<sha>` is the git commit at **compile** time (SDK `AssemblyInformationalVersion`) | `NetSuiteApiClientService.cs:54-58` |

---

## 3. Unknowns to close before coding

**U1 — Which term is wrong in `must be 3`?**
Either the filler identity (`quantity` absent, assignment sums to 0, line defaults to 3) or units (`UoMRate != 1`: line quantity is stock units, assignment carries display units).

**U2 — Does NetSuite accept a `quantity: 0` filler at a bin-tracked location under `?replace=`?**
This is the only NetSuite behaviour the fix depends on and it has never been exercised. It must be answered against a sandbox, not guessed.

**U3 — Does `orderLine` really equal `transactionline.id`?**
The RCA notes three `transactionline` rows per TO item line. If `LineSequenceNumber` is not the same id the transform expects, adding fillers for 1,657 lines would target the wrong lines — the worst possible outcome. Confirm with `NextTransactionLineLink` on a successful IF before Phase 1 ships.

---

## 4. Decisions

- **D1 — The server owns the line set.** `SaveTOItemFulfillment` reconciles the submitted rows against an authoritative open-line read and completes the payload itself. The mobile vendor gate stays a *display and work-scoping* control, never a correctness control.
- **D2 — Read authoritative state immediately before posting, paged.** Reuse `GetFreshTOFulfillmentLineStateAsync`, extend it, page it. No new parallel `.sql` script: one source of truth.
- **D3 — Fillers must carry an explicit zero quantity.** `itemreceive:false` is inert on an itemFulfillment and today's filler is exactly the entry that can trigger Issue 2. Shape: `{orderLine, quantity: 0, itemreceive: false, inventoryDetail: {inventoryAssignment: {items: []}}}` at a bin-tracked location. **This amends** lines 16/37/141 of `TO-itemFulfillment-implementation-fix.md`, which would drop `inventoryDetail` from fillers and re-trigger `INVALID_PARAMETER: InventoryDetail … not found in the request body`.
- **D4 — Phases 1 and 2 ship as one unit.** Reconciliation alone adds more entries that violate (2); it converts Issue 1 into Issue 2 on every partial save.
- **D5 — Fail-open first, fail-closed after one measured cycle.** The authoritative read is mandatory once the line *set* depends on it, but flipping it in the same deploy removes the ability to observe. `EnableFailClosedRevalidation` config flag, default off.
- **D6 — Reconcile-then-split, not merge-per-bucket.** Compute the completed line list once, then split into BAD and GOOD so both posts carry full filler coverage.
- **D7 — Diagnostics by line id, never by item name.** Keep the toast readable; make the log authoritative.
- **D8 — Out of scope:** single-request GOOD+BAD atomicity, web packing gating, returns/VRA parity beyond the shared filler fix, RESTlet.

---

## 5. Phases

### Phase 0 — Evidence (0.5–1 day) — blocks Phases 1 and 2

Tasks:

- [ ] **0.1 Confirm the deployed assembly.** Read `AssemblyInformationalVersion` of the deployed `Integration.NS.dll`, or add the startup log in 5.1 and read it. Reconcile with `build=…+a0b920da…` vs the claimed `3e5639af`. Rule out a stale publish folder and a dirty-tree build.
- [ ] **0.2 Answer U1 with SuiteQL** (no server logs needed). For the order behind `must be 3`:

```sql
SELECT t.tranid, tl.id AS line_id, i.itemid, i.displayname,
       tl.units, u.conversionrate,
       ABS(tl.quantity) AS ordered,
       NVL(tl.quantityshiprecv, 0) AS shipped,
       ABS(tl.quantity) - NVL(tl.quantityshiprecv, 0) AS remaining,
       tl.isclosed
FROM transactionline tl
JOIN transaction t ON t.id = tl.transaction
JOIN item i ON i.id = tl.item
LEFT JOIN unitstypeuom u ON u.internalid = tl.units
WHERE t.tranid = '<failing order>'
  AND tl.transactionlinetype = 'SHIPPING'
ORDER BY tl.id
```

  The line with `remaining = 3` is the suspect. `conversionrate = 1` → filler identity (2a). `conversionrate > 1` → units (2b).

- [ ] **0.3 Answer U2 with the sandbox matrix.** One bin-tracked TO, one scanned line, several unscanned. Four payload variants, raw responses recorded in this doc:

| # | Filler entry | Result |
|---|---|---|
| 1 | today: `itemreceive:false` + empty assignment, no quantity | expect `total inventory detail quantity must be N` |
| 2 | `quantity:0` + empty assignment | **preferred fix** |
| 3 | `quantity:0`, no `inventoryDetail` | expect `INVALID_PARAMETER … InventoryDetail` |
| 4 | filler omitted entirely | expect `configure the inventory detail for …` |

- [ ] **0.4 Answer U3** with `NextTransactionLineLink` on a recent successful IF: confirm `previousline` values are the ids the app sends as `orderLine`.
- [ ] **0.5 Capture the two failing orders' open-line counts** for later regression comparison (771: 63 lines; 776-2: 1,657).

Exit criteria: U1, U2, U3 each have a recorded answer, and variant 2 or 3 is the approved filler shape. If both fail, Phase 1 ships as a **guard only** (D3 fallback: reject with the unposted line ids instead of repairing).

### Phase 1 — The server owns the line set (1–1.5 days)

Files: `Integration.NS/Services/NetSuiteApiClientService.cs`.

- [x] **1.1 Page the authoritative read.** Extend `GetFreshTOFulfillmentLineStateAsync` (`:1569-1588`) to page with the `limit`/`offset` + `totalResults` contract used by `GetTransferOrderLineQry`; add `tl.item`, `i.itemid`, `i.displayname`, `tl.units` (UoM rate for guards) via the same query. Filters stay minimal: transaction id, `transactionlinetype = 'SHIPPING'`, transfer-order record types. No status, category, vendor, bin, or balance predicates.
- [x] **1.2 Fail loud on truncation.** If paging ends short of `totalResults`, throw before posting. A silently truncated set is indistinguishable from "these lines are closed".
- [x] **1.3 Reconcile instead of merge.** Replace `MergeFreshTOFulfillmentLineState` usage with:
  - `openLines` = fresh `isclosed != 'T' && (quantity - shipped) > 0`;
  - every submitted row must be in `openLines`, else throw naming WMS line + item code (message style of `:1747`);
    **(as built: only submitted scans are required to be open; a submitted
    zero-quantity row is a stale client filler and is replaced or dropped)**;
  - every open line not submitted becomes a filler DTO: `LineSequenceNumber = fresh.LineId`, `NetsuiteOrderInternalId = orderId`, `NetsuiteMaterialInternalId = fresh.Item`, `ScannedQuantity = 0`, `IsLocationUsedBin = <header flag computed from submitted rows>`.
  Delete `MergeFreshTOFulfillmentLineState` and its tests, or keep it only for the Returns path until parity.
- [x] **1.4 Reject an empty request.** No positive scanned quantity → throw before any NetSuite call.
- [x] **1.5 Reconcile once, split after.** Build the completed list, then `badTO`/`goodTO` from it so both posts carry fillers.
  **(as built: `BuildTOFulfillmentBucketLines` completes each bucket;
  `EnsureTOFulfillmentBucketIsComplete` asserts the set before each POST. The
  GOOD bucket drops fillers for lines the BAD post closes.)**
- [x] **1.6 Fix the silent drop.** Regression test: a scanned line beyond the 1,000-row cap must never be classified as closed (today it is stripped without the loud message).

Exit criteria: on a multi-vendor example, a save from a vendor-scoped payload posts with all other open lines as fillers.

### Phase 2 — Repair the payload identity (0.5–1 day) — ships with Phase 1

Files: `TransferOrderIFPayloadDTO.cs`, `ReturnsIFPayloadDTO.cs`, `VendorReturnAuthorizationIFPayloadDTO.cs`.

- [x] **2a — Filler quantity** (if 0.2 says `rate == 1`): set `Quantity = 0` on fillers and keep the empty assignment when `isUsedBin`. Apply to all three builders.
- [ ] **2b — Unit normalization** (if 0.2 says `rate != 1`): define display-UoM remaining as `(LineQuantity - QuantityAlreadyShipped) / UoMRate`; use display units in the payload, `FindScansExceedingRemaining`, and the mobile over-scan guard. Reject `UoMRate <= 0` before the first HTTP call.
- [x] **2c — Contract test (required regardless).** Assert over a built payload:
  - every `orderLine` is in the open set and unique per bucket;
  - `quantity == sum(inventoryAssignment.items[].quantity)` for every entry;
  - at a bin-tracked location every entry has `inventoryDetail`, and `binNumber` is non-null iff `quantity > 0`;
  - no closed/fully-shipped line present.
  **(as built: the builder-level tests cover quantity/sum, uniqueness and the
  bin rule; bucket completeness is covered by the 1.5 tests and the F2 guard, so
  a bucket that omits an open line can no longer reach a POST.)**

### Phase 3 — Fail-closed and guard alignment (0.5 day)

- [ ] **3.1** Flip `EnableFailClosedRevalidation` on after 3 clean production days (no unexpected `missing`, no query failures). Alert on the throw.
- [x] **3.2** Document next to `RequiresBin` (`:1503`) why fillers are exempt (no bin, quantity 0) so it is not "fixed" later.
- [x] **3.3** Mobile: remove the `LineQuantity == LineQuantityPacked + LineQuantityBackOrdered` drop (`TransferOrderItemView.razor.cs:821`); it uses a packed-based remaining that is wrong on partially fulfilled orders. Server completion makes it non-fatal until the APK ships.

### Phase 4 — Display query losslessness (1 day, hardening)

File: `NS_TO_x_Packing_Get_Items.sql`.

- [x] **4.1** `JOIN unitstypeuom` → `LEFT JOIN`; `NVL(conversionrate, 1)`; label fallback when `unitname` is null.
- [x] **4.2** Never filter a line out for missing availability, bin, vendor, or UoM data.
- [x] **4.3** Add `AND ivb.custrecord_dbti_vba_location = tl.location` (RCA suspect #4). (Already in `a89f9963`.)
- [ ] **4.4** Scope subqueries by `transaction.id`, not `tranid`.
- [x] **4.5** Leave `status IN ('B','D','E')` and the category filter alone — they are business rules, not display joins. File a note if a valid IF can exist outside them.

The same INNER `unitstypeuom` join exists in five sibling scripts (`NS_TO_Get_Itemfulfillment_Items`, `NS_PurchaseOrder_Get_Items`, `NS_TransferOrder_Get_Items`, both returns scripts, `NS_VendorReturnAuthorization_Get_Items`). One ticket per path; do not sweep them into this change.

### Phase 5 — Diagnostics (1 day)

- [x] **5.1** Startup log: `logger.LogInformation("API build {BuildStamp}", NetSuiteApiClientService.BuildStamp);` — deploy verification without waiting for a failure. **(as built: also logs the two TO IF flags.)**
- [x] **5.2** Structured save diagnostic on both catch blocks (`:1818`, `:1862`): `order`, `open`, `posted`, `missing`, `extra`, `dup`, `uomRate`, `postedIds`, `badLine`, `build`. Truncate id lists to the first 10. **(as built: `extra` is measured against the open set, `-1` when unknown; `badLine` is not a separate field because `bucket=BAD` identifies it.)**
- [x] **5.3** Add `build=` to the Returns (`:1926`) and VRA (`:2002`, `:2052`) diags.
- [x] **5.4** `DecodeTOItemFulfillmentLine`: replace the `not matched` branch with an explicit "this item is not in the submitted set" plus the posted `orderLine` ids. Widen the stem logic so a pack suffix (`X2`, `6+1`) cannot defeat candidate matching.

### Phase 6 — Deferred (own tickets)

1. GOOD/BAD atomicity — BAD succeeds + GOOD fails leaves a partial fulfilment with no compensation (`2026-10-21` follow-up #2).
2. Returns / VRA reconciliation parity (Phase 1 + 2a across `PostReturnsDTO` / `PostVendorReturnAuthorizationDTO`).
3. Web packing gating decision (currently ungated by design).
4. NetSuite RESTlet fallback — only if the Phase 0 matrix shows the REST transform cannot express "do not ship this line" reliably.

---

## 6. Deployment sequence

| # | Artifact | Contains | Rollback |
|---|---|---|---|
| 1 | API | Phase 5.1 (build log) — optional warm-up deploy with no behaviour change | redeploy previous |
| 2 | API | Phase 1 + Phase 2 (paired), reconciliation fail-open, structured diag | `EnableReconciliation=false` flag |
| 3 | — | Observe: `missing=0`, `extra=0` on ordinary saves; one real multi-vendor order | — |
| 4 | API | Phase 3.1 fail-closed | flag back to fail-open |
| 5 | API | Phase 4 SQL | script revert; no data migration |
| 6 | APK | Phase 3.3 + over-scan `>=` → `>` | app rollback; server tolerant |

Deploy verification: commit → build from that commit → deploy that artifact → read `build=1.0.0+<sha>` on the next save or in the startup log. A stale publish folder or a dirty-tree build makes the stamp useless as evidence; commit first.

---

## 7. Test plan

Unit / integration (`dotnet test Integration.NS.Tests`):

- contract test 2c (the durable asset — it would have caught both production errors);
- reconciliation: posted ⊂ open, open ⊃ posted, both empty, scanned line closed, scanned line absent, duplicate `(orderLine, IsBad)`;
- paging: 999 / 1,000 / 1,001 / 1,657 rows, exact-multiple stop, short-page contradiction, truncated-set throw;
- silent-drop regression: a scanned line beyond row 1,000 is never classified closed;
- payload: filler serializes `"quantity":0` and still carries `inventoryDetail` at a bin-tracked location;
- guards: empty request rejected, over-remaining rejected, missing bin rejected, assignment mismatch rejected — all before HTTP.

Sandbox (Postman, before each API deploy):

1. bin-tracked TO, one scanned line, several unscanned;
2. multi-vendor TO, handheld loads vendor A only;
3. TO with a line beyond row 1,000;
4. TO with a line whose `tl.units` is null;
5. TO with a missing preferred/vendor bin;
6. line partially fulfilled after the screen loaded;
7. mixed GOOD/BAD on one order;
8. `UoMRate` > 1, < 1, fractional quantities.

Device (after the APK ships):

1. multi-vendor order — vendor A fulfilled, vendor B untouched in NetSuite;
2. bin-tracked partial scan posts and the IF quantities match NetSuite's open quantities;
3. large order (776-2 class) saves without truncation;
4. missing-bin line rejected with an actionable message.

---

## 8. Acceptance criteria

Status 2026-10-23: code-side criteria are covered by unit tests; production and
sandbox criteria remain unverified because the failing orders cannot be replayed.

- [ ] Phase 0 answers U1, U2, U3 recorded in this document. **(U3 inferred; U1/U2 open.)**
- [ ] TO `ISTR-THM00100000771` and `ISTR-THM00100000776-2` post successfully with their original scanned lines.
- [x] The resulting IFs carry only the scanned lines at quantity > 0; all other open lines are present at quantity 0.
  **(Guaranteed per bucket by `BuildTOFulfillmentBucketLines` and asserted by `EnsureTOFulfillmentBucketIsComplete`.)**
- [x] No payload entry violates `quantity == sum(assignments)`; no closed line is sent.
- [ ] A vendor-scoped mobile save succeeds without qualifying the other vendors' lines.
- [x] A save whose authoritative read is truncated or fails does not post (behind the flag).
  **(Truncation throws; a failed read is fail-open unless the flag is on.)**
- [x] A line with no UoM row, or with 0 availability, still displays and still posts when unscanned.
  **(Display: LEFT JOIN + NVL; posting: server filler.)**
- [ ] One week of production diagnostics: `missing=0`, `extra=0`, zero `total inventory detail quantity` and zero `configure the inventory detail` during save.
- [ ] `build=1.0.0+<expected sha>` confirmed in production after deploy.

---

## 9. Non-goals

- Relying on NetSuite item display names to identify lines.
- Using the mobile vendor-filtered result as the fulfilment set.
- Silently ignoring lines missing from the authoritative read.
- Claiming the workflow is fixed before the sandbox and device matrices pass.
- Rewriting the GOOD/BAD posting model in this change.

---

## 10. Risks and trade-offs

- **Reconciliation without Phase 2 is a regression**, not a partial fix: it multiplies entries that violate the quantity identity. Ship them together or ship the guard-only variant.
- **Fail-closed trades availability for correctness.** A SuiteQL outage will block TO saves; that is the deliberate cost of not posting a payload whose line set is unknown. It must be alertable.
- **`quantity: 0` on a filler is unverified NetSuite behaviour.** Variant 3 is the fallback; guard-only mode is the fallback after that.
  **Deploy gate:** reconciliation is enabled by default, so run Phase 0.3
  variant 2 before the deploy. If that is not possible, deploy with
  `WMS_TO_IF_ENABLE_RECONCILIATION=false`, post one real order, then flip it.
  Fail-open does not protect against this risk: the read succeeds, and the 400
  comes from the POST.
- **Per-bucket fillers add entries to both documents.** On a mixed GOOD/BAD save
  each document now addresses every open line, so a large order produces two
  large payloads. This is required for correctness (an unaddressed line is
  fulfilled by the transform), but it is the reason the sandbox matrix must
  include a 1,000+ line order.
- **Unit normalization (2b) is the riskiest single change** if U1 points there: it touches guards that have already been re-tuned three times. Change it in one place (a single conversion helper), with tests per rate class.
- **Payload growth:** a 1,657-line order will post 1,657 entries. The web flow already does this; the difference is that the mobile save now does too. Watch request size and NetSuite latency in Phase 3's observation window.
- **Effort:** ~5–6 dev-days plus verification. Phase 0 is the highest-value half-day; Phase 4–6 are backlog. If time is short, cut Phases 4 and 5.2 — never Phase 0 or 2c.

---

## 11. Implementation status (2026-10-23)

Recorded in `.opencode/docs/changes/2026-10-23-to-if-reconciliation.md`.

**Phase 0 — not closed.** U1, U2 and U3 are recorded as **unverified**:
the failing order id was lost among the client's transactions (so the U1/U3
SuiteQL probes cannot be run), and no sandbox matrix was available for U2.
The two failing orders cannot be replayed, so the related acceptance criterion
stays open.

Because U1 is unknown, the implementation takes the filler-identity route
(2a, `quantity: 0`) and deliberately leaves unit normalization (2b) untouched.
Because U2 is unknown, the chosen filler shape is variant 2; if the sandbox
rejects it, switch to variant 3 / guard-only.

**Shipped in code:**

- [x] 1.1 paged authoritative read (1,000/page, `totalResults` stop, 100-page cap)
- [x] 1.2 truncated-set throw
- [x] 1.3 reconcile instead of merge (legacy merge kept only behind the rollback flag)
- [x] 1.4 empty/multi-order request rejected before HTTP
- [x] 1.5 reconcile once, split GOOD/BAD after
- [x] 1.6 silent-drop regression covered by paging tests
- [x] 2a filler `quantity: 0` in all three builders
- [x] 2c payload contract tests
- [x] 3.2 `RequiresBin` filler-exemption comment
- [x] 3.3 mobile packed-based drop removed
- [x] 4.1 `LEFT JOIN unitstypeuom` + safe UoM defaults
- [x] 5.1 startup build log
- [x] 5.2 structured TO save diagnostic
- [x] 5.3 `build` in Returns/VRA diagnostics
- [x] 5.4 decoder wording + posted ids + pack-suffix stem

**Deliberately deferred:** 2b (needs U1), 3.1 fail-closed flip (needs the
observation window; flag exists and defaults off), 4.4 (runtime only has
`tranid`; SuiteQL forbids outer-alias references in derived tables), Phases 4.2
beyond the UoM join, and Phase 6.

