# TO Item Fulfillment — server-owned line set and filler identity

Date: 2026-10-23
Area: `Integration.NS/Services/NetSuiteApiClientService.cs`,
`Application.DataTransferObjects/Transactions/Packing/NS/Payload/*IFPayloadDTO.cs`,
`Integration.NS/NSScripts/NS_TO_x_Packing_Get_Items.sql`,
`Mobile.MAUI/Components/Pages/Packing/TransferOrder/TransferOrderItemView.razor.cs`,
`Api.CoreWebAPI/Program.cs`
Type: fix (correctness) — packing → STR item fulfillment
Plan: `.opencode/docs/TO-itemFulfillment-resolution-plan.md`

## Summary

The mobile forwards its loaded rows unchanged (scanned and zero-quantity). The
API keeps the rows the operator scanned, reconciles them against an
authoritative, **paged** read of the order's open SHIPPING lines, and completes
the itemFulfillment payload itself: the scanned lines at their scanned quantity,
every other open line as a zero-quantity filler. The vendor gate stays a
display/work-scoping control and is no longer a correctness control.

Two payload-contract violations were live:

1. a line the payload never addressed (the transform ships it anyway, with no
   inventory detail → `Please configure the inventory detail for <item>`);
2. a line whose `quantity` and inventory detail disagreed (the filler kept the
   transform's pre-filled quantity while its assignment summed to 0 →
   `The total inventory detail quantity must be N`).

## Phase 0 — open questions (recorded, not verified)

Phase 0 evidence could not be captured in this change:

| # | Question | Status | Why / what is recorded |
|---|---|---|---|
| U1 | Is `must be 3` a filler identity error or a UoM-rate error? | **Unverified** | The failing order id was lost among the client's transactions, so the SuiteQL probe cannot be run. The implementation therefore takes the plan's identified filler-identity route (2a, `quantity: 0`) and does **not** change unit semantics. 2b stays deferred until U1 is answered. |
| U2 | Does NetSuite accept a `quantity: 0` filler with an empty assignment at a bin-tracked location under `?replace=`? | **Refuted in field (2026-10-23, same day as deploy)** | Answered by order `STR-THM00100000881/226006` before any sandbox matrix could run: the transform rejected the GOOD bucket with `[USER_ERROR] Value 0.0 outside of valid min/max range for field quantity` (`replace=True usedBin=True posted=1022 scannedLines=6` — i.e. 1,016 zero-quantity fillers). Variant 2 is dead. The filler reverted to variant 1 (no `quantity` field, field-proven Transaction A 2026-10-15); see [2026-10-23 IF filler quantity-zero refutation fix](../../.github/specs/2026-10-23-to-if-filler-quantity-zero-refuted-fix.md). |
| U3 | Does `orderLine` equal `transactionline.id`? | **Unverified** | `NextTransactionLineLink` was not inspected. The pre-existing, production-working scanned lines already use `transactionline.id` as `orderLine`; fillers use the same `LineId` from the same table, so the change does not introduce a new identity assumption. Confirm before the first large-order deploy. |

The two failing orders (`ISTR-THM00100000771`, `ISTR-THM00100000776-2`) cannot be
replayed, so the acceptance criterion "they post successfully with their original
scanned lines" remains unverified.

## What changed

### Phase 1 — the server owns the line set

- `GetFreshTOFulfillmentLineStateAsync` now pages the authoritative read
  (1,000 rows/page, 100-page cap, `totalResults`-driven stop) and throws instead
  of returning a truncated set. The query carries `tl.item`, item code/name and
  `NVL(conversionrate, 1)` for diagnostics and future guards; it has no status,
  category, vendor, bin, UoM or availability predicates.
- `ReconcileTOFulfillmentLines` replaces the merge as the default path:
  - every submitted **scan** must be in the current open set, otherwise the save
    throws naming WMS line, item and scanned quantity;
  - submitted zero-quantity rows are discarded (they are stale client fillers);
  - every other open line becomes `{orderLine, quantity: 0}` with fresh item
    identity, `ScannedQuantity = 0`, `IsBad = false` and the header bin flag;
  - fresh ordered/shipped quantities overwrite the device snapshot before the
    existing over-remaining guard runs.
- An all-zero request and a multi-order request are rejected before any
  NetSuite call (`ValidateTOFulfillmentRequest`, unit-tested without HTTP).
- Reconciliation happens once; the GOOD/BAD split happens after it
  (`BuildTOFulfillmentBucketLines`). **Each bucket** is completed with a
  zero-quantity filler for every open line it does not own, because GOOD and BAD
  are separate transforms - completing only the reconciled list left the BAD
  payload addressing just its own scans, which is the original defect in another
  shape. The GOOD post runs second, so it drops fillers for lines the BAD post
  closes (a closed line has no sublist entry).

### Phase 2a — filler identity

All three builders (`TransferOrderIFPayloadDTO`, `ReturnsIFPayloadDTO`,
`VendorReturnAuthorizationIFPayloadDTO`) now emit `quantity: 0` on fillers and
keep the empty `inventoryDetail` at bin-tracked locations.

> **Correction (2026-10-23, later the same day): Phase 2a refuted in the field.**
> The `quantity: 0` filler was rejected by the transform with
> `Value 0.0 outside of valid min/max range for field quantity`. All three
> builders reverted to the field-proven shape: fillers carry **no `quantity`
> property** (`WhenWritingNull` omits the null), keeping the empty assignment.
> The "must be N" motivation for Phase 2a remains unverified (U1) and is now
> suspected to be a UoM-rate issue (Phase 2b), not filler identity. See
> [2026-10-23 IF filler quantity-zero refutation fix](../../.github/specs/2026-10-23-to-if-filler-quantity-zero-refuted-fix.md).

### Phase 3.3 / Phase 4 — client and display query

- Mobile `SaveScan` no longer drops a line using
  `LineQuantity == LineQuantityPacked + LineQuantityBackOrdered`; it forwards
  the loaded rows (scanned and zero-quantity) unchanged, so the API-side
  reconciliation decides and the rollback flag still receives its fillers.
- The display query's `unitstypeuom` join is now `LEFT JOIN` with
  `NVL(conversionrate, 1)` and `NVL(unitname, 'EACH')`. A line with missing UoM
  data displays instead of disappearing.
- The `ivb` location predicate was already added in `a89f9963`.
- Phase 4.4 (scope aggregate subqueries by `transaction.id`) is deferred:
  the packing request carries only `tranid`, and SuiteQL forbids an outer-alias
  reference from a derived table.

### Phase 5 — diagnostics

- Startup log: `API build {BuildStamp} | TO IF reconciliation=… failClosed=…`,
  so the deployed stamp and the deployed flags are readable from the first log
  line.
- Structured TO save diagnostic: `bucket`, `replace`, `usedBin`, `order`,
  `open`, `submitted`, `posted`, `scannedLines`, `missing`, `extra`, `dup`,
  `uomRate`, first 10 `postedIds`, `build`. `extra` is **measured** against the
  authoritative open set (`-1` when the read failed); it is no longer a
  hardcoded `0`, which could never fail the observation criterion it exists for.
  `uomRate` now comes from the save-time read, not the device snapshot.
- A deterministic duplicate collapse is logged as a warning on success too
  (input rule 4), not only embedded in a failed-save diagnostic.
- Returns and VRA diagnostics now include `build`.
- The non-position decoder says `is not in the submitted set` and prints the
  posted `orderLine` ids; item-name matching is explicitly display-only.
- `RequiresBin` documents why zero-quantity fillers are exempt.

## Flags

Environment variables, matching the service's existing credential style:

| Variable | Default | Purpose |
|---|---|---|
| `WMS_TO_IF_ENABLE_RECONCILIATION` | `true` | Set `false` to roll back to the pre-reconciliation freshness merge. |
| `WMS_TO_IF_ENABLE_FAIL_CLOSED_REVALIDATION` | `false` | Set `true` to refuse the save when the authoritative read fails (D5, flip after the observation window). |

## Second review pass (same day)

The first implementation reconciled once but split naively (`badTO`/`goodTO`
filtered by `IsBad`), so only the GOOD payload carried fillers. Because GOOD and
BAD are separate transforms, the BAD payload still left every other open line
unaddressed - RCA issue 1 in a new shape. This pass closes that gap:

- `BuildTOFulfillmentBucketLines` completes **each** bucket, and
  `EnsureTOFulfillmentBucketIsComplete` asserts (F2) that the bucket addresses
  exactly the open set it is responsible for, exactly once, immediately before
  the POST. A missing line, an extra line or a duplicate `orderLine` now fails
  the save with the ids named, instead of posting a payload NetSuite will reject.
- The GOOD post is skipped when it has no positive scan. Fillers make `goodTO`
  non-empty by construction, so the old `Any()` guard would have posted a
  zero-quantity document (and, before the URL fix, one whose body carried
  `inventoryDetail` while the URL omitted `?replace=`).
- The transform URL is selected by the header bin flag alone, matching the body,
  which always carries `inventoryDetail` at a bin-tracked location.
- Paging stops on `hasMore == false`. A full final page with `totalResults`
  absent no longer probes a past-end offset (404) and silently disables
  reconciliation under fail-open.
- The two request guards are extracted and unit-tested; the environment-flag
  tests are isolated in a non-parallel xUnit collection.

## Tests

`dotnet test Integration.NS.Tests` — 574 passing at Phase 2a, 575 passing after
the same-day Phase 2a revert (refutation trait added):

- reconciliation: posted ⊂ open, open ⊃ posted, closed/fully-shipped exclusion,
  fresh quantity overwrite (including the UoM rate), GOOD+BAD split, stale client
  filler replacement, filler identity, empty submitted set;
- bucket completeness (1.5/D6): BAD carries a filler for every open line it does
  not scan, GOOD addresses a still-open bad line at zero, GOOD drops a filler for
  a line the BAD post closes, and the F2 guard rejects both a missing line and a
  duplicate `orderLine`;
- save guards (1.4): empty, all-zero, two orders, order id 0, and the passing
  case;
- paging: 0/999/1,000/1,001/1,657/2,000/2,001 rows, exact-multiple stop, short
  page contradiction, full page with `hasMore=false`, page failure, runaway loop;
- payload contract (2c): a shipping entry's `quantity` equals its assignment sum,
  unique `orderLine` per bucket, bin present iff quantity > 0, fillers serialize
  **without a `quantity` property** (field-proven shape; the `quantity: 0`
  variant was refuted 2026-10-23) and keep the empty `inventoryDetail` at
  bin-tracked locations, including a byte-exact snapshot of the used-bin filler
  entry.

## Still open

0. ~~**GOOD/BAD combined-remaining guard**~~ **Addressed 2026-10-23:**
   `EnsureScansWithinPackableQuantity` runs the per-row + good+bad aggregate
   check on both transports, with all guard quantities normalized to the line's
   display unit (`PackableDisplayQuantity`). This closes the over-scan bug
   (STR-THM00100000867 root cause: boxes vs eaches comparison); see
   [2026-10-23 TO packing over-scan fix](2026-10-23-to-packing-overscan-fix.md).
   RESTlet fallback and returns/VRA reconciliation parity remain open items.

1. ~~**U2 (sandbox) is a deploy gate.**~~ **Answered in the field 2026-10-23: variant
   2 (`quantity: 0` + empty assignment) is rejected by the transform.** Phase 2a
   reverted to variant 1 (no `quantity` field — Transaction A's field-proven
   shape). Any future filler wire-shape change must pass the sandbox matrix
   first; see [2026-10-23 IF filler quantity-zero refutation fix](../../.github/specs/2026-10-23-to-if-filler-quantity-zero-refuted-fix.md).
   Variant 3 and guard-only remain the fallbacks if variant 1 ever fails.
2. U1 remains open (2b deferred). U3 is answered by inference: both the display
   query and the authoritative read use `transactionline.id` as `orderLine`, and
   scanned lines already post with it in production - record that rather than
   treating it as an unknown.
3. The two failing orders cannot be replayed, so `ISTR-THM00100000771` /
   `ISTR-THM00100000776-2` remain unverified end to end.
4. Fail-closed flip after three clean production days (one week recommended on
   the acceptance list).
5. Phase 4.4 and the five sibling display scripts (own tickets).
6. GOOD/BAD atomicity, returns/VRA reconciliation parity, RESTlet fallback.
