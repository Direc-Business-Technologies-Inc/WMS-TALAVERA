# Tasks — Mobile Inventory Worksheet Manual Input

**Date:** 2026-10-14
**Status:** Confirmed
**Mode:** FEATURE
**Spec:** [design.md](design.md) (sibling)
**Goal:** Add manual item entry to the mobile Inventory Worksheet page, reusing the existing scan pipeline.
**Architecture summary:** Changes confined to `Mobile.MAUI/Components/Pages/InventoryCounting/InventoryWorksheet/CreateInventoryWorksheetView.razor(.cs)`. Manual entries reuse `ICItems`, the Good/Bad toggle, and the existing save endpoint. No backend, DTO, payload-builder, or web changes. No branching/committing per user instruction.

## Verified technical context

- Target: `Mobile.MAUI` (MAUI Blazor Hybrid, Radzen, `BaseComponent`, `AppAction` pattern).
- Endpoints consumed (existing, unchanged): `GET /InventoryCounting/Worksheet/Items`, `POST /InventoryCounting/Worksheet/SaveScan`, `GET /Lookup/Susidiary/Locations`, `GET /Lookup/BinLocations`.
- No mobile test project exists; verification is manual per design.md.

## Global constraints

- Follow the file's existing `AppAction` + `ActionFactory.ExecuteAppActionAsync` pattern; no direct `HttpClient` calls outside `Client`.
- Do not modify or delete commented blocks (`.razor.cs` lines ~31, ~124-140, ~220-235, ~250-359) — preserved per Req P4.
- All UI disabled-states follow the existing `ActionSaveScan?.Busy == true` pattern.
- Toasts via existing `Toast.Warning` / `Toast.Error` helpers.
- `Web.BlazorServer` files are read-only references (parity/regression comparison only).

## Phase A — Item list loading

### Task A1: Restore the item-list AppAction
`_Requirements: 1.2_`

- File: `Mobile.MAUI/Components/Pages/InventoryCounting/InventoryWorksheet/CreateInventoryWorksheetView.razor.cs:124-140` — the commented block shows the intended implementation; re-add it as **live code below the comment** (leave the comment itself in place per P4).
- Field `private AppAction<List<InventoryItemVM>>? ActionGetInventoryItems` already exists at line 22 — wire it up.
- Action body: `await Client.Get<List<InventoryItemVM>>("/InventoryCounting/Worksheet/Items")`; `OnSuccess` populates `Data` and calls `await InvokeAsync(StateHasChanged)`.
- **Done when:** action compiles and is executable via `ActionFactory`.

### Task A2: Execute on first render
`_Requirements: 1.2_`

- File: `CreateInventoryWorksheetView.razor.cs:202-246` (`OnAfterRenderAsync`, `firstRender` block).
- Add guarded execution mirroring the `ActionGetLocations` call at lines 215-218.
- **Done when:** opening the page populates `Data` (verify via debugger breakpoint or temporary toast).
- **Checkpoint:** run Task D2 build check before proceeding to Phase B.

## Phase B — Manual-add UI

### Task B1: Add manual-entry fields
`_Requirements: 1.3, 1.5_`

- File: `CreateInventoryWorksheetView.razor.cs`
- Add: `private InventoryItemVM? ManualItem { get; set; }`, `private decimal ManualQuantity { get; set; }`.
- **Done when:** fields compile; no behavior change yet.

### Task B2: Add manual-add action item to the scan-actions panel
`_Requirements: 1.2, 5.2_`

- File: `CreateInventoryWorksheetView.razor:231-323` (the `!IsActionPanelCollapsed` block).
- Add a `scan-action-item` stack (same structure as Move/Remove, lines 254-286): icon (e.g. `edit`), label "Manual", click toggles the manual-entry area.
- Manual-entry area: searchable `RadzenDropDown` bound to `ManualItem` (`Data=@Data`, `TextProperty=MaterialName`, `ValueProperty=NetsuiteMaterialInternalId`, `AllowFiltering=true`, `FilterCaseSensitivity.CaseInsensitive`), numeric `RadzenNumeric<decimal>` bound to `ManualQuantity`, Add button → `AddManualItem()`.
- Disabled states: dropdown/inputs/button disabled when `ActionSaveScan?.Busy == true` or `ActionGetInventoryItems?.Busy == true` (Req 5.2).
- **Done when:** panel renders on device; item list is filterable.

### Task B3: Implement `AddManualItem()`
`_Requirements: 1.3, 1.4, 1.5, 2.1, 2.2, 2.3_`

- File: `CreateInventoryWorksheetView.razor.cs` (new method, near `ProcessScannedItem`, line ~439).
- Logic (per design Phase 2 confirmed sketch):
  1. Guards: location (2.1), required bin (2.2), item/quantity (2.3) → `Toast.Warning` + return.
  2. Find line by `NetsuiteMaterialInternalId` + `CurrentBinInternalId`; create if absent (copy field-mapping from `ProcessScannedItem`, lines 418-425).
  3. Apply `ManualQuantity` to `GoodScannedQuantity` or `BadScannedQuantity` per `NextScanIsBad`; `line.ScanCount++`.
  4. Clear `ManualItem` / `ManualQuantity`; `await InvokeAsync(StateHasChanged)`.
- **Done when:** CP1, CP2, CP3 verified manually on device.
- **Checkpoint:** run Task D1 matrix rows for CP1–CP3 before Phase C.

## Phase C — Validation and location safety

### Task C1: Extend `SaveScan` validation
`_Requirements: 3.1_`

- File: `CreateInventoryWorksheetView.razor.cs:442-460` (`SaveScan`).
- Add after the `ICItems.Count == 0` check:
  `if (ICItems.Any(x => x.GoodScannedQuantity < 0 || x.BadScannedQuantity < 0)) { await Toast.Warning("Quantities cannot be negative."); return; }`
- **Done when:** CP4 verified — rejection happens before any HTTP call.

### Task C2: Location-change confirmation
`_Requirements: 4.1, 4.2_`

- File: `CreateInventoryWorksheetView.razor.cs:604-627` (`OnLocationChanged`).
- Current-to-target: at method start, capture previous `SelectedLocationInternalId`. If `ICItems.Count > 0`, show confirm dialog (Radzen `DialogService` or the existing confirm pattern used by `ActionFactory` confirm:true): on cancel, revert `SelectedLocationInternalId` to previous and return; on confirm, `ICItems.Clear()` before proceeding to bin reload. Empty-`ICItems` path keeps current behavior (4.2).
- **Done when:** CP5, CP6 verified.

## Phase D — Verification

### Task D1: Manual verification matrix
`_Requirements: 1.1-5.2, P1-P3_`

- Run on device/emulator; record results:
  - CP1/CP2: manual add new + existing item (mixed with a scan).
  - CP3: all three guards.
  - CP4: negative quantity save rejection.
  - CP5/CP6: location change cancel/confirm with lines present.
  - CP7: save a mixed session; confirm payload shape via API logs.
  - CP8: scan-only session end-to-end unchanged (P1); no-bin location session (P3).
- **Done when:** all matrix rows pass.

### Task D2: Build check
`_Requirements: all_`

- `dotnet build` from solution root; only `Mobile.MAUI` projects should report changes.
- **Done when:** build succeeds with zero new warnings in changed files.

## Traceability summary

| Requirement | Tasks |
|---|---|
| 1.1–1.5 | A1, A2, B1, B2, B3 |
| 2.1–2.3 | B3 |
| 3.1, 3.2 | C1 (3.2 preserved by existing guards) |
| 4.1, 4.2 | C2 |
| 5.1, 5.2 | B2, B3 |
| P1–P4 | D1 |

**Optional work:** none — all tasks are required. (Automated unit tests deferred to a future spec by decision; not marked Optional here because they are out of scope, not optional MVP work.)
