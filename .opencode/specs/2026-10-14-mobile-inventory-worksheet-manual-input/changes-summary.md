# Summary of Changes — Mobile Inventory Worksheet Manual Input

**Date:** 2026-10-14
**Spec:** `.opencode/specs/2026-10-14-mobile-inventory-worksheet-manual-input/` (design.md + tasks.md, Confirmed)
**Spec review:** Approved before implementation — task line references (`.razor.cs:124-140`, `202-246`, `439`, `442-460`, `604-627` and razor panel lines) matched the actual file contents; architecture decision (reuse `ICItems`, `NextScanIsBad`, existing save endpoint; no backend change) was sound.
**Status:** Implemented (Phases A–C). Phase D (device verification matrix) remains manual.

## Files changed

Only the two mobile page files, exactly per spec scope:

1. `Mobile.MAUI/Components/Pages/InventoryCounting/InventoryWorksheet/CreateInventoryWorksheetView.razor.cs`
2. `Mobile.MAUI/Components/Pages/InventoryCounting/InventoryWorksheet/CreateInventoryWorksheetView.razor`

No backend, DTO, payload-builder, or web changes. All commented blocks preserved untouched (Req P4).

## Changes by task

### A1 — Item-list AppAction restored
- `ActionGetInventoryItems` (already declared as a field) is now instantiated with the exact body from the preserved comment: `GET /InventoryCounting/Worksheet/Items`; `OnSuccess` populates `Data` + `StateHasChanged`. The original commented block remains in place.

### A2 — First-render execution
- In `OnAfterRenderAsync` (`firstRender`), the action executes after the auth/subsidiary read, mirroring the `ActionGetLocations` guard pattern. The commented call block remains below it.

### B1 — Manual-entry fields
- Added: `InventoryItemVM? ManualItem`, `decimal ManualQuantity`, plus `bool ShowManualEntry` (drives panel visibility; spec left the toggle detail open).

### B2 — Manual-add UI in scan-actions panel
- New "Manual" `scan-action-item` (icon `edit_note`) between Remove and All-list items; toggles the manual-entry area.
- Manual-entry area (inside the `!IsActionPanelCollapsed` block): searchable `RadzenDropdown` bound to `ManualItem` over `Data` (`TextProperty=MaterialName`, `AllowFiltering=true`, case-insensitive), `RadzenNumeric<decimal>` for quantity, Add button → `AddManualItem()`.
- All three inputs disabled when `ActionSaveScan?.Busy == true || ActionGetInventoryItems?.Busy == true` (Req 5.2).
- Razor file gained `@using Shared.Libraries.ViewModel.Common` so `TValue="InventoryItemVM"` resolves in markup (build fix; type was previously only referenced via code-behind).

### B3 — `AddManualItem()`
- Guards: missing location (2.1), missing required bin (2.2), missing item or quantity `<= 0` (2.3) → `Toast.Warning` + return.
- Finds/creates line by `NetsuiteMaterialInternalId` + `CurrentBinInternalId`, copying field mapping from `ProcessScannedItem`.
- Applies `ManualQuantity` to `GoodScannedQuantity` (bad if `NextScanIsBad`), increments `line.ScanCount`, clears `ManualItem`/`ManualQuantity`, `StateHasChanged`.
- Existing scan handlers (`ProcessScannedItem`, `MoveScan`, `NegateScannedItem`) untouched (P1 / CP8).

### C1 — Save validation
- `SaveScan` now rejects negative Good/Bad quantities (`"Quantities cannot be negative."`) after the empty-list check, before any HTTP call (CP4). Req 3.2 intact since manual UI cannot produce negative quantities.

### C2 — Location-change confirmation
- `OnLocationChanged` captures the previous location; if `ICItems.Count > 0`, shows `Dialog.Confirm` (service injected via `BaseComponent`). Cancel → revert `SelectedLocationInternalId`, no state change (CP5). Confirm → `ICItems.Clear()`, then proceed to the existing bin reload (CP6). Empty-list path unchanged per 4.2.

## Verification done
- `dotnet build Mobile.MAUI/Mobile.MAUI.csproj -f net9.0-android` — **Build succeeded, 0 errors**; no new warnings in the changed files (all warnings are pre-existing in unrelated files).
- Note: `graphify update` not available on PATH in this session (skipped).

## Outstanding (Phase D, manual on device)
- CP1–CP6: manual add new/existing item, guards, negative-quantity save rejection, location change cancel/confirm.
- CP7: mixed/scan-only/manual-only sessions save the same payload shape (API logs).
- CP8: scan-only regression + no-bin location session.
