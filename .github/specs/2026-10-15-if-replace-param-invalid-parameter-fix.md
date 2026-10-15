# Fix: NetSuite 400 INVALID_PARAMETER on Packing Item Fulfillment (`replace=item.inventoryDetail.inventoryAssignment`)

Date: 2026-10-15
Status: Implemented, third iteration — awaiting client verification
Mode: Bugfix / Follow-up
Scope: Packing module — Item Fulfillment (STR, Returns packing, RTS / Vendor Return Authorization)
Related: [Mobile auth logout + IF sublist RCA](2026-10-14-mobile-auth-logout-and-if-sublist-rca.md); `.opencode/docs/mobile-netsuite-4xx-catalog.md`
Changed files:
- `Integration.NS/Services/NetSuiteApiClientService.cs`
- `Application.DataTransferObjects/Transactions/Packing/NS/Payload/TransferOrderIFPayloadDTO.cs`
- `Application.DataTransferObjects/Transactions/Packing/NS/Payload/ReturnsIFPayloadDTO.cs`
- `Application.DataTransferObjects/Transactions/Packing/NS/Payload/VendorReturnAuthorizationIFPayloadDTO.cs`

## Summary

Packing saves on mobile fail with:

```
Action failed: Error while posting GOOD TO Item Fulfillment. NetSuite 400 Bad Request:
Bad Request(400):
[INVALID_PARAMETER] Resource or sub-resource 'inventoryDetail' from the query
parameter replace was not found in the request body. Provide a valid resource name
in the query parameter or include the resource in the request body.
[diag replace=True usedBin=True scannedLines=1 totalLines=25]
```

The confirmed root cause: on a **bin-tracked location**, the fulfillment transform URL carries `?replace=item.inventoryDetail.inventoryAssignment`, but the payload covered that sub-resource on **only the scanned lines** (1 of 25). The 24 zero-qty filler lines were posted as `{orderLine, itemreceive:false}` with no `inventoryDetail`, and NetSuite's `replace` validation on the **itemFulfillment** transform requires the sub-resource to be present on the body's item entries consistently — it did not find it and rejected the transform.

The 400 survived two earlier fixes because each one addressed a different link in the chain (see Timeline). This document records the full investigation, the three-part fix now in source, and what remains unproven.

## Timeline — why this error kept coming back

| Commit / date | Change to the fulfillment payload / URL | Symptom |
|---|---|---|
| `668cc85e` (original packing build) | **No `inventoryDetail` on any line**, no `?replace=` — NetSuite auto-manages inventory detail | Worked for ordinary items; **400 `Please configure the inventory details in line N`** at bin-tracked locations (the error remembered from web) |
| `54505798` (2026-07-14) | Added flat REST-shaped `inventoryDetail.inventoryAssignment` on scanned lines **+** `?replace=` on the URL | Fixed the bin error; **introduced** the INVALID_PARAMETER whenever the bucket mixes detail and no-detail lines |
| `bb6d5a39` / `1a100a9` (2026-08-14) | Removed `?replace=` from the URL | Back to "invalid sublist" class errors |
| `4b062dcd` (2026-08-26) | Switched detail back to SOAP-shaped `inventoryAssignmentList` wrapper | Wrong shape for REST v1 |
| `85c94246` (2026-10-14, RCA fix) | Restored flat REST shape **+** unconditional `?replace=` | INVALID_PARAMETER — the error under investigation |
| This fix (2026-10-15) | Conditional `replace`, legacy shape for non-bin, **empty assignment on filler lines at used-bin** | Awaiting client verification |

Key insight from the history: the two eras each solve one half of the problem. "Then" (no detail, no replace) worked everywhere except bin-tracked locations. "Now" (detail + replace) works for bin-tracked lines but breaks when the payload mixes lines with and without the sub-resource. Neither commit was wrong on its own.

## Root Cause (confirmed)

1. **Location is bin-tracked** → the transform needs explicit inventory detail per fulfilled line, and `?replace=` so NetSuite replaces its prefilled assignment sublist instead of merging with it.
2. **Payload builders only attach `inventoryDetail` to lines with `ScannedQuantity > 0`.** Zero-qty filler lines (`{orderLine, itemreceive:false}`) had no detail.
3. A GOOD bucket with **1 scanned line out of 25** therefore produced a body where `inventoryDetail` existed on 1 entry only. NetSuite's `replace` validation on the `itemFulfillment` transform reports `'inventoryDetail' ... was not found in the request body`.

Cross-check: the Returns **receipt** (`ReturnsIRPayloadDTO` + `ItemReceiptUrl + "?replace=item.inventoryDetail.inventoryAssignment"`, `NetSuiteApiClientService.cs:1020`) uses the **same filler-line pattern and the same replace param** and works in production. The mix is acceptable on the `itemReceipt` transform; the `itemFulfillment` transform enforces the sub-resource strictly. That difference is why the "same fix" keeps recurring on fulfillment only.

## Investigation path (how it was confirmed)

1. **Initial hypothesis (partly wrong):** the GOOD bucket's guard was `goodTO.Any()` rather than `goodTO.Any(x => x.ScannedQuantity > 0)`, so an all-zero bucket posted with `?replace=` and no detail anywhere. A conditional URL was implemented.
2. **Client retest failed** with the identical error — ruling out stale deployment and refuting the all-zero-bucket theory (the screenshot showed scanned quantities).
3. **Server logs were unavailable** (tested on the client's device/environment), so on-device diagnostics were added: every fulfillment failure now appends `[diag replace=… usedBin=… scannedLines=… totalLines=…]` to the error the mobile toast already shows, plus a full `ILogger.LogError` of the URL and serialized payload server-side.
4. **Client repro returned `[diag replace=True usedBin=True scannedLines=1 totalLines=25]`** — bin-tracked location, detail on 1 of 25 entries, `replace` correctly present. Mixed-coverage root cause confirmed.
5. **Git archaeology** against the original working build (`668cc85e`) showed the "then" payload had no `inventoryDetail` at all, motivating the legacy-path split for non-bin locations.

## The Fix (three parts, all in source)

### 1. Legacy shape for non-bin locations (then-behavior, proven)

`TransferOrderIFPayloadDTO` / `ReturnsIFPayloadDTO` / `VendorReturnAuthorizationIFPayloadDTO` — scanned lines at `!isUsedBin` now emit the exact `668cc85e` shape:

```json
{ "orderLine": n, "itemreceive": true, "quantity": q }
```

no `inventoryDetail`, letting NetSuite auto-manage it. The URL for those posts drops `?replace=` (`ItemFulfillmentUrlNoReplace`).

### 2. Full sub-resource coverage at bin-tracked locations

Zero-qty filler lines at `isUsedBin` now carry an **empty assignment** so the replace target exists on every body entry:

```json
{ "orderLine": n, "itemreceive": false, "inventoryDetail": { "inventoryAssignment": { "items": [] } } }
```

Scanned lines keep the explicit assignment (status, quantity, bin).

### 3. URL selection keyed to payload content (`NetSuiteApiClientService.cs`)

All five fulfillment posts (TO bad/good, Returns, VRA bad/good) choose the URL with the same predicate as the payload shape:

```csharp
// replace + explicit inventoryDetail only when the location is bin-tracked AND the bucket carries scanned lines
var usedBinWithScan = isUsedBin && goodTO.Any(x => x.ScannedQuantity > 0);
var url = string.Format(usedBinWithScan ? ItemFulfillmentUrl : ItemFulfillmentUrlNoReplace, "transferOrder", orderId);
```

`ItemFulfillmentUrlNotUsedBin` was a byte-identical duplicate of `ItemFulfillmentUrl` and is no longer used by the fulfillment paths.

### 4. Diagnostics (temporary, remove after verification)

- On-device suffix: `[diag replace={bool} usedBin={bool} scannedLines={n} totalLines={n}]` appended to every fulfillment failure message.
- Server-side: `logger.LogError(ex, "… Item Fulfillment failed. Url: {Url}. Payload: {Payload}", …)` in all five catch blocks (the class already had an injected `ILogger`).

## Behavior Matrix (current code)

| Location | Scanned line | Filler line (qty 0) | URL | Status |
|---|---|---|---|---|
| Not bin-tracked | `{orderLine, itemreceive:true, quantity}` | `{orderLine, itemreceive:false}` | no `replace` | Proven (matches `668cc85e`) |
| Bin-tracked | `+ inventoryDetail.inventoryAssignment[status, qty, bin]` | `+ inventoryDetail.inventoryAssignment.items: []` | `?replace=item.inventoryDetail.inventoryAssignment` | **Unverified — awaiting client repro** |

Not affected: PO Item Receipt, TO receiving (restlet), Returns Item Receipt — different URLs and payloads, untouched.

## Verification Plan

Build verified locally: `dotnet build Integration.NS` → **0 errors** (pre-existing nullable warnings only). Behavior must be confirmed on the client:

1. **Used-bin STR with partial scans** (the failing case: 1 of 25 lines) — must save. This is the scenario the fix targets.
2. **Used-bin STR with good + bad lines** — both buckets save; verify quantities, bins, and statuses (`C`/`B` per trip-ticket exemption).
3. **Non-bin STR** — legacy path; must behave as in the original working era.
4. **All-zero-qty save** — previously 400; must now post without `replace`.
5. **Returns packing and RTS** — same payload/URL paths.
6. **PO Item Receipt sanity check** — untouched path.

### Reading the next `[diag …]` result

| diag | Meaning | Next action |
|---|---|---|
| Success (no error) | Empty assignment satisfies the replace validation | Remove diags after sign-off |
| `replace=True usedBin=True` + new error about empty/invalid assignment | NetSuite rejects `items: []` on fillers | Next variant: drop `replace` for used-bin and rely on the transform's prefilled detail (i.e. merge-aware payloads), or a zero-qty assignment item — decided with data in hand |
| `replace=True usedBin=True` + same INVALID_PARAMETER | Replace semantics are stricter than modeled | Stop client deploy cycles; replay payloads against a sandbox TO via Postman with 4 variants (replace on/off × detail coverage) |
| `replace=False` on a used-bin post | `IsLocationUsedBin` data gap | Investigate the location flag rather than the payload |

### Pre-test housekeeping

Per the 2026-10-14 RCA: BAD posts before GOOD with no compensation. A failed GOOD post can leave a transfer order partially fulfilled, and a naive retry then fails differently. **Reconcile the affected TO in NetSuite before re-testing**, and prefer a scratch/test transfer order over a live shipment.

## Decisions and Alternatives Rejected

| Decision | Alternatives rejected and why |
|---|---|
| **Restore the `668cc85e` shape for non-bin locations** (chosen) | *Keep detail everywhere*: the `replace` validation then applies to every non-bin post and reintroduces the failure class. |
| **Empty assignment on filler lines at used-bin** (chosen) | *Omit filler lines*: unlisted lines default to being fulfilled — fillers are mandatory. *Zero-qty assignment items*: likely rejected as invalid inventory detail; kept as fallback. |
| **Keep `?replace=` for used-bin** (chosen) | *Drop replace everywhere*: reinstates the "invalid sublist" errors the July fix resolved. |
| **Diagnostics in the thrown message** (chosen) | Server-only logging was useless — the test runs on the client's environment with no log access. |
| **Skip all-zero-qty posts entirely** (deferred) | Business decision (what should a "nothing scanned" save mean?), not a bug fix. |
| **BAD/GOOD compensation** (deferred) | Design decision; needs a pre-post TO status check or post-failure reconciliation. |

## Residual Risk — next likely 4xx

1. **High — empty-assignment rejection.** If NetSuite refuses `items: []` on filler lines, the used-bin path needs a different shape (see matrix above). The diag distinguishes this from other failures immediately.
2. **High — bin-0 lines.** 4xx catalog 1a: a binned location where both `NetsuiteMaterialVendorAssignedBin` and `NetsuiteMaterialPrefferedBinId` are `0` posts `inventoryDetail` without `binNumber` → `Please configure the inventory details in line N`. Also, `InventoryAssignmentItem` has **no lot/serial fields at all** (`PayloadsDTO.cs:67-77`), so lot- or serial-tracked items cannot satisfy inventory-detail requirements regardless of these fixes.
3. **Medium — sequence integrity.** BAD-then-GOOD with no compensation leaves partially fulfilled TOs; retries fail confusingly.
4. **Medium — duplicate `orderLine`.** A line scanned as both good and bad produces two entries with the same `orderLine` (IF builders do no `GroupBy`; the PO receipt does — `PurchaseOrderIRPayloadDTO.cs:42`). Catalog 1b.
5. **Low–medium — other modules.** Receipts clean; Inventory Counting / Worksheet untouched and not reported failing.

## Follow-up Actions

| # | Action | Status |
|---|---|---|
| 1 | Conditional `replace` keyed to `isUsedBin && scanned` (5 posts) | **Done** |
| 2 | Legacy payload shape for non-bin locations (3 payload builders) | **Done** |
| 3 | Empty assignment on filler lines at used-bin (3 payload builders) | **Done — awaiting client verification** |
| 4 | Payload + URL logging in fulfillment catch blocks | **Done** |
| 5 | On-device `[diag …]` markers | **Done — remove after sign-off** |
| 6 | Guard the bin-0 / missing-`binNumber` case (catalog 1a); add lot/serial fields if tracked | Open |
| 7 | Dedupe `orderLine` in IF builders (catalog 1b) | Open |
| 8 | BAD/GOOD compensation for partial fulfillment | Deferred — design decision |
| 9 | Close-out semantics for all-zero-qty saves | Deferred — business decision |
| 10 | Reconcile the TOs left partially fulfilled by earlier failed attempts | Pending |
| 11 | `graphify update .` to refresh the knowledge graph (blocked in this session) | Outstanding |

## Validation Limits

- Live NetSuite behavior is still unverified for the current fix. The confirmation that exists is the on-device diag from the **client's** environment: `replace=True usedBin=True scannedLines=1 totalLines=25` on a bin-tracked location.
- The claim that the `itemReceipt` transform tolerates the same filler-line mix is based on the Returns IR path being in production use with that payload shape; it was not A/B tested in this session.
- Whether NetSuite accepts `inventoryAssignment.items: []` is unproven — it is the error message's own suggested remedy, applied to full coverage, not a confirmed NetSuite behavior.
