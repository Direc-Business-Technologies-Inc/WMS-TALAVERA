# Fix: PO Item Receipt uses fresh quantities and validates combined scans

Date: 2026-10-27  
Status: Implemented in source; focused PO tests and Windows mobile build pass. Full integration suite has intermittent failures in unrelated timing-sensitive tests.  
Mode: Bugfix / defence in depth  
Scope: Receiving module — mobile PO scan state and GOOD/BAD PO Item Receipt (`SavePOItemReceipt` → `purchaseOrder/{id}/!transform/itemReceipt`)

Related:
- [2026-10-17 PO Item Receipt closed-line guard](2026-10-17-po-item-receipt-closed-line-guard-fix.md)
- [2026-10-19 PO Item Receipt line fan-out and fillers](2026-10-19-po-item-receipt-line-fanout-and-fillers-fix.md)
- [2026-10-19 PO filler inventory-detail fix](2026-10-19-po-item-receipt-filler-inventory-detail-fix.md)

Changed files:
- `Integration.NS/Services/NetSuiteApiClientService.cs` — fresh PO-line read and pre-post reconciliation
- `Mobile.MAUI/Components/Pages/Receiving/PurchaseOrder/PurchaseOrderItemView.razor.cs` — scan revalidation after refresh, manual entry, and before save; scan/post mutual exclusion
- `Integration.NS.Tests/Services/NetSuiteApiClientServicePOReceiptFreshValidationTests.cs` — fresh-state validation regressions

## Summary

The PO receipt request can become stale between loading lines and posting. Mobile state can also become inconsistent after restoring scans on Refresh or entering quantities manually. The previous server guard compared each submitted GOOD or BAD row separately against quantities sent by the device, so it did not guarantee that their combined total fit the latest PO balance.

The API now reads the current PO transaction lines from NetSuite immediately before receipt construction and validates the submission before either BAD or GOOD post. The mobile screen also checks quantities at the points where stale or excessive values can be introduced.

## Root cause and guard gap

`SavePOItemReceipt` previously used the request's `LineQuantity`, `LineQuantityReceived`, and `UoMRate` as authoritative. These values were loaded earlier on the device and could be stale if another receipt was posted, if scans were restored after Refresh, or if quantities were manually edited. The previous over-remaining check inspected rows independently, not the aggregate GOOD + BAD quantity for a PO line.

This made the existing API rejection reachable and correct for that request, but it did not protect against stale client state or a split-quality quantity whose individual buckets passed while their combined quantity exceeded the remainder.

## Fix

### API pre-post reconciliation

Before any REST transform call, the API queries raw item PO lines by internal order id. The read is paged using the existing SuiteQL page helper; only active PO statuses are accepted. The query intentionally avoids bin/vendor display joins so those joins cannot fan out or drop source lines.

The validator:
- Requires exactly one valid PO in the request.
- Matches submitted lines by transaction line id and item id.
- Requires the submitted UoM rate to match the current PO UoM; it does not reinterpret an already-scanned quantity after a unit change.
- Sums GOOD and BAD scan quantities per line and compares the total to `(ordered - received) / current UoM rate`.
- Rejects negative quantities, duplicate same-quality rows, unrecognized lines, missing open-line fillers, invalid quantity data, and submissions with no still-open scanned lines.
- Refreshes quantity fields from NetSuite on accepted rows before the established receipt path continues.

All checks run before either receipt bucket posts. Existing closed-line behavior, receipt ordering (BAD then GOOD), open-line fillers, bin assignment, and `?replace=` payload construction are retained. Existing NetSuite payload fixes are not bypassed or rewritten.

### Mobile validation

The PO screen now rejects negative and over-remaining combined GOOD/BAD scans after manual entry and before Save. It repeats the check after Refresh restores scan state against newly loaded quantities. A post-in-progress flag prevents additional barcode scans, manual edits, and Refresh while Save is underway. Quantities are not silently capped or altered.

## Behaviour matrix

| Situation | Result |
|---|---|
| Current GOOD + BAD total is within the fresh remainder | Existing receipt payload path proceeds |
| Another receipt reduced the balance after the device loaded the PO | Rejected before any receipt post; operator must reload and reconcile scans |
| GOOD and BAD each fit individually but their sum exceeds the remainder | Rejected before any receipt post |
| Scanned quantity is on a closed/unknown/mismatched PO line | Rejected before any receipt post |
| Open PO line is missing from submitted fillers | Rejected before any receipt post; the service does not rebuild the payload |
| Current UoM differs from the device's scan UoM | Rejected; reload and rescan |
| NetSuite write fails after submission | Error tells the operator to check existing receipts before retrying; separate BAD/GOOD writes are not atomic |

The quantity rejection remains intentional: invalid quantities must not post. This change is designed to prevent stale or combined-quantity gaps from reaching the transform and to surface those conditions earlier; it cannot prevent another system from changing the PO after the fresh read or make separate NetSuite writes atomic.

## Regression coverage and verification

New tests cover stale client quantities, combined GOOD + BAD overage, exact remaining quantity, quantity refresh, preservation of open fillers, closed lines, missing fillers, duplicate same-quality rows, and UoM changes. Existing PO closed-line and payload suites remain in place.

Verified during implementation:
- Focused PO receipt/guard tests: 40 passed.
- Integration tests excluding two known timing-sensitive tests: 708 passed.
- The two excluded integration tests passed individually.
- `Mobile.MAUI` Windows target (`net9.0-windows10.0.19041.0`) built with 0 errors.
- `graphify update .` completed.

The Windows target build is not a substitute for a device-specific Android/iOS build or production NetSuite field verification. Before retrying an affected production PO, reconcile existing item receipts and submit only quantities still outstanding.
