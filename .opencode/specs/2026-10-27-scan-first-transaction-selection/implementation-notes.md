# Scan-First Transaction Selection — Implementation Notes

| Metadata | Value |
|---|---|
| Date | 2026-10-27 |
| Status | Implemented |
| Mode | FEATURE |
| Scope | Receiving PO, TO, Returns; Packing TO, Returns, Vendor Return Authorization |
| Related | [requirements.md](requirements.md), [design.md](design.md), [tasks.md](tasks.md) |

## Decision correction

The original confirmed requirements and design excluded browsing pending transactions. Operator feedback clarified that pending-list access is required as a fallback. This implementation therefore keeps scan/manual single-transaction lookup as the primary path and restores pending-list retrieval only after an explicit Refresh action. Pending lists are not fetched automatically when these selectors open.

## Shipped behavior

- Restored the Refresh button, pending-list display, local text filtering, and clear-search behavior on all six selectors.
- Restored the appropriate operation-specific pending-list endpoints; the existing scan/manual lookup calls and navigation remain available.
- Restored paged loading for Receiving Transfer Orders (50 rows per request, with Load more).
- Displayed list-loading progress and recoverable list-load errors. Refresh is disabled while its current request is active.
- Removed the unused Receiving Purchase Order JavaScript observer/disposal path, which was only associated with the removed list observer.

## Changed paths

- `Mobile.MAUI/Components/Pages/Receiving/PurchaseOrder/PurchaseOrderView.razor(.cs)`
- `Mobile.MAUI/Components/Pages/Receiving/TransferOrder/TransferOrderView.razor(.cs)`
- `Mobile.MAUI/Components/Pages/Receiving/Returns/ReturnsView.razor(.cs)`
- `Mobile.MAUI/Components/Pages/Packing/TransferOrder/TransferOrderView.razor(.cs)`
- `Mobile.MAUI/Components/Pages/Packing/Returns/ReturnsView.razor(.cs)`
- `Mobile.MAUI/Components/Pages/Packing/VendorReturnAuthorization/VendorReturnAuthorizationView.razor(.cs)`

## Verification

- `dotnet build Mobile.MAUI/Mobile.MAUI.csproj -f net9.0-windows10.0.19041.0 --no-restore` — passed with 0 errors. The build reports existing nullable/async warnings in mobile components.
- Interactive device verification (initial page makes no list call; Refresh fetches and re-fetches; filtering, pagination, scan/manual lookup, and route navigation) remains to be performed.
