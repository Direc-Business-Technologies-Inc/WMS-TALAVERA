# Fix: PO Item Receipt omits `?replace=item.inventoryDetail.inventoryAssignment` (bin-tracked 400)

Date: 2026-10-17
Status: Fix in source, build clean, 385/385 tests green — **awaiting client verification on the original failing PO**
Mode: Bugfix / RCA
Scope: Receiving module — GOOD/BAD PO Item Receipt (`SavePOItemReceipt` → `purchaseOrder/{id}/!transform/itemReceipt`)
Related:
- [2026-10-15 IF replace-param fix](2026-10-15-if-replace-param-invalid-parameter-fix.md) — established the `?replace=` contract and the "receipts tolerate the filler mix, fulfillment does not" distinction
- [2026-10-16 packing bin guard false-positive fix](2026-10-16-packing-bin-guard-false-positive-fix.md) — listed "PO Item Receipt" as *not affected*; that assumption is corrected here
- [2026-10-14 mobile auth/logout and IF sublist RCA](2026-10-14-mobile-auth-logout-and-if-sublist-rca.md) — source of the working Returns receipt shape
Changed files:
- `Integration.NS/Services/NetSuiteApiClientService.cs`
- `Integration.NS.Tests/Services/NetSuiteApiClientServicePOReceiptUrlTests.cs` (new)

## Summary

Receiving a purchase order at a **bin-tracked location** fails at the last step:

```
Action failed: Error while posting GOOD PO Item Receipt. NetSuite 400 Bad Request:
Bad Request(400): [USER_ERROR] Error while accessing a resource.
Please configure the inventory detail in line 2 of the item list.
```

The failing PO is internal id `171264` (`tranid PO-THM00100000223-TR`), entity `1718`
(vendor `RSC DISTRIBUTION INCORPORATED`), location `7`, subsidiary `2`, `orderstatus = E`
(partially received), `iteminventorydetailhidden = T`, 93 lines.

The message "*Please configure the inventory detail in line N*" is the same one the
2026-10-15 spec traced on the fulfillment path. Here it is raised by NetSuite because the
**PO item receipt transform was never told to apply the inventory assignment the payload
carries** — the URL lacks `?replace=item.inventoryDetail.inventoryAssignment`, so NetSuite
ignores the `inventoryDetail` in the body and falls back to its own empty, auto-generated
assignment, which a bin-tracked line cannot satisfy.

## How it was found (investigation path)

1. **Confirmed the failing path.** `PurchaseOrderItemView.razor.cs` → `POST /api/Receiving/PurchaseOrder/SaveScan`
   → `PurchaseOrderController.PurchaseOrderSaveScan` → `PostPurchaseOrderIRCmd` → `SavePOItemReceipt`.
   The error string `Error while posting GOOD PO Item Receipt` is thrown only at
   `NetSuiteApiClientService.cs` inside `SavePOItemReceipt`, so the PO path — not the TO/ICTO
   restlet path — is the one failing.
2. **Eliminated the data-gap hypothesis.** The operator's SuiteQL for PO 171264 showed both bin
   ids on the failing lines: `VendorBinAssignmentId` empty/`0`, `NetsuiteMaterialPrefferedBinId = 8`,
   `usebins = 'T'`. A bin **is** resolvable, so this is not the "no bin resolved" case (catalog
   case 1a) — the payload should carry `binNumber: { id: "8" }`.
3. **Verified the bin flag round-trips.** The API emits PascalCase (`WebApplicationService`,
   `PropertyNamingPolicy = null`); the mobile copies `LocationUsedBin` (`PurchaseOrderItemView.razor.cs`)
   and `TransactionVM.IsLocationUsedBin => LocationUsedBin == "T"` is a get-only property that
   `System.Text.Json` serializes by default; the server binds the settable `PostPurchaseOrderDTO.IsLocationUsedBin`.
   So `line.IsLocationUsedBin` reaches the builder as `true` and the builder emits bin `8`.
4. **Found the asymmetry.** The PO builder **always** emits `inventoryDetail.inventoryAssignment`
   (`PurchaseOrderIRPayloadDTO.cs:69-97`), but `SavePOItemReceipt` posts to `ItemReceiptUrl`
   (`NetSuiteApiClientService.cs:53`) **without** `?replace=`. The working Returns receipt posts the
   **same `itemReceipt` transform** with `?replace=item.inventoryDetail.inventoryAssignment`
   (`SaveReturnsItemReceipt`), and the spec records it as working in production.
5. **Ruled out a regression.** `git log -S "replace=item.inventoryDetail.inventoryAssignment"` shows the
   token was only ever added to the *fulfillment* URLs and the Returns receipt URL — **no commit ever
   added it to the PO receipt URL**. This is a latent gap, exposed the first time a PO was received
   into a bin-tracked location.

## Root Cause

The transform URL and the payload body were **incoherent**:

| Path | Transform | Body carries `inventoryDetail`? | URL declares `?replace=`? | Result at bin-tracked location |
|---|---|---|---|---|
| Returns receipt | `transferOrder/!transform/itemReceipt` | yes (always) | **yes** | works (in production) |
| **PO receipt** | `purchaseOrder/!transform/itemReceipt` | yes (always) | **no** | **400 — "configure the inventory detail in line N"** |
| IF fulfillment (legacy, non-bin) | `…/!transform/itemFulfillment` | no (by design) | no | works (no bins needed) |

On a `!transform`, the child record's sublists are pre-generated from the source. To override the
`inventoryDetail.inventoryAssignment` sub-resource you must declare `replace=`; without it NetSuite
keeps its own (empty) assignment. The PO payload therefore *sent a bin that NetSuite never applied*.
The 2026-10-16 spec's "PO Item Receipt — not affected" line was an assumption about the packing
*guard*, never an analysis of the PO receipt URL.

## The Fix

`Integration.NS/Services/NetSuiteApiClientService.cs`:

- `SavePOItemReceipt` now computes the header-level flag and selects the URL through a new helper:

  ```csharp
  var isUsedBin = Data.Any(x => x.IsLocationUsedBin);
  string url = BuildPOItemReceiptUrl(isUsedBin, orderId);
  ```

  ```csharp
  internal static string BuildPOItemReceiptUrl(bool isUsedBin, int orderId) =>
      string.Format(
          isUsedBin ? ItemReceiptUrl + "?replace=item.inventoryDetail.inventoryAssignment" : ItemReceiptUrl,
          "purchaseOrder",
          orderId);
  ```

  Bin-tracked locations get `?replace=…`; non-bin locations keep the plain transform (byte-identical to
  today). This mirrors the Returns receipt (`SaveReturnsItemReceipt`) and the fulfillment URL-selection idiom.

- Both catch blocks now **log the URL and serialized payload** and append the same
  `[diag replace=… usedBin=… scannedLines=… totalLines=…]` suffix the fulfillment paths use, so a single
  client repro is decisive instead of a blind retry cycle.

No change to `PurchaseOrderIRPayloadDTO` — its shape was already correct.

## Behaviour Matrix (after fix)

| Location | Scanned line, bin resolved | Filler line (qty 0) | URL | Status |
|---|---|---|---|---|
| Bin-tracked | `+ inventoryDetail.inventoryAssignment[status "1", qty, binNumber]` | `{orderLine, itemreceive:false}` (no detail) | `?replace=item.inventoryDetail.inventoryAssignment` | **Awaiting client repro** |
| Not bin-tracked | `+ inventoryDetail.inventoryAssignment[status "1", qty, binNumber:null]` | `{orderLine, itemreceive:false}` | plain transform (no replace) | unchanged from today |

The mix (detail on received lines, none on fillers) is acceptable on the `itemReceipt` transform — the
Returns receipt proves it in production; the 2026-10-15 spec explains why `itemFulfillment` is stricter.

## Tests

- **New:** `Integration.NS.Tests/Services/NetSuiteApiClientServicePOReceiptUrlTests.cs` — pins that
  `BuildPOItemReceiptUrl(isUsedBin: true, …)` contains `?replace=item.inventoryDetail.inventoryAssignment`
  and that `isUsedBin: false` does not.
- **New + existing PO payload tests:** `PurchaseOrderIRPayloadDTOTests` (13 cases) still green — the
  payload builder was not touched.
- Full `Integration.NS.Tests`: **385/385 passed** (was 332 at the 2026-10-16 spec; 53 tests added since).
- Build: `dotnet build Integration.NS.Tests/Integration.NS.Tests.csproj` → **0 errors** (warnings pre-existing).

## Verification Plan

Must be confirmed on the client — the failure reproduces only against the live NetSuite account.

1. **Reproduce the original PO (171264) at location 7.** A bin-tracked location; the good receipt must
   now post. Expected diag on the mobile toast if it still fails: `[diag replace=True usedBin=True …]`
   with `binNumber:{"id":"8"}` in the server-side `Payload` log.
2. **If it still fails**, read the decision table below against the diag + logged payload.
3. **Non-bin PO sanity check** — an ordinary PO at a `usebins='F'` location must still post (URL unchanged,
   `replace=False`).
4. **Reconcile first:** BAD posts before GOOD with no compensation, so a previously half-posted receipt
   must be corrected in NetSuite before retesting. Prefer a scratch PO over a live vendor delivery.

| Observed | Meaning | Next action |
|---|---|---|
| Passes | Fix correct | close |
| `replace=True usedBin=True` + same error | replace semantics differ on `purchaseOrder/!transform` vs `transferOrder/!transform` | replay the logged payload against a sandbox PO (Postman) with 4 variants: replace on/off × detail coverage |
| `replace=True usedBin=False` | `IsLocationUsedBin` not arriving | fix the mobile/DTO flag plumbing (`TransactionVM:17` → `PostPurchaseOrderDTO:22`) |
| `replace=True usedBin=True` + `binNumber` missing from payload | builder drop | re-check `PurchaseOrderIRPayloadDTO.cs:81-92` gate |

## Decisions and Alternatives Rejected

| Decision | Alternative rejected and why |
|---|---|
| **Gate the URL on header-level `Data.Any(x => x.IsLocationUsedBin)`** | *Unconditional `?replace=`* (match Returns exactly): equally defensible — Returns proves it works — but a PO is single-location, so the gate costs nothing and guarantees zero change for working non-bin PO receipts. |
| **Add the diag + payload log now** | *Ship the one-line URL fix alone*: if the real cause is the `IsLocationUsedBin` plumbing instead, we would learn nothing from a bare retry; the diag makes one repro decisive. |
| **Leave `PurchaseOrderIRPayloadDTO` untouched** | *Also touch the payload builder*: it already emits the correct shape (identical to `ReturnsIRPayloadDTO`); the defect is purely the URL. |
| **Fix the PO path only** | *Also re-audit `SaveTOItemReceipt` (restlet, `script=1853`)*: it uses a different mechanism (restlet, not `!transform`) and was not implicated; out of scope. |

## Residual Risk / Follow-ups

1. **Medium — unverified live.** No NetSuite sandbox was used; the fix rests on the Returns-receipt
   precedent (same transform family) plus the code/`git log` analysis, not on an observed passing PO post.
2. **Medium — the mirror-image flag case is silent.** If `LocationUsedBin` arrives as `"Y"`/`"1"`/null,
   `IsLocationUsedBin` is `false`, no `replace` is chosen, and the exact same `line N` error recurs with
   `usedBin=False` as the only clue. Consider a warning when a line carries bin ids but `IsLocationUsedBin`
   is false (also recommended in the 2026-10-16 spec's residual risk #1).
3. **Low — other parameters on the PO transform.** If a future PO advice/partial-receipt scenario needs
   `?replace=item` as well, this fix does not cover it; only the inventory-assignment sub-resource is addressed.
4. **Known pre-existing — BAD-before-GOOD with no compensation.** Unchanged; retries after a partial post
   can double-receive. Reconcile in NetSuite before retesting.

## Appendix — Key references

- `Integration.NS/Services/NetSuiteApiClientService.cs` — `SavePOItemReceipt` (URL now via `BuildPOItemReceiptUrl`),
  `SaveReturnsItemReceipt` (the `?replace=` precedent), `ItemReceiptUrl` constant.
- `Application.DataTransferObjects/Transactions/Receiving/NS/Payload/PurchaseOrderIRPayloadDTO.cs:69-97` — payload builder.
- `Application.DataTransferObjects/Transactions/Receiving/NS/Payload/ReturnsIRPayloadDTO.cs:82-85` — working shape.
- `Application.DataTransferObjects/Transactions/Commons/NS/Payload/PayloadsDTO.cs` — `InventoryDetail` / `InventoryAssignment`.
- `Shared.Libraries/ViewModel/Common/TransactionVM.cs:17` — `IsLocationUsedBin`.
- `Mobile.MAUI/Components/Pages/Receiving/PurchaseOrder/PurchaseOrderItemView.razor.cs` — `SaveScan`, `LocationUsedBin` copy.
