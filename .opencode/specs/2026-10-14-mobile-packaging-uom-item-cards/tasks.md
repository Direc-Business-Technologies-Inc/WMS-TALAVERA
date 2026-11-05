# Tasks — Mobile Packaging UoM on Item Cards (Receiving, Counting, Worksheet)

**Date:** 2026-10-14
**Status:** Confirmed
**Mode:** FEATURE
**Spec:** `design.md` (sibling, Phase 2 — confirmed)
**Goal:** Render the already-available `UoMName` on every in-scope Mobile item card, matching the Packing pattern, with Mobile-only Worksheet plumbing.
**Architecture summary:** Display-only Razor insertions (`RadzenText`, `Subtitle2 / .6rem`) bound to existing VM properties; two 1–2-line C# copies in `CreateInventoryWorksheetView.razor.cs` (scan → line → summary). No Web/API/DTO/Mapster/SQL/NetSuite changes.
**Verified technical context:**
- Target: `Mobile.MAUI` (Blazor Hybrid), `.razor` markup + partial `.razor.cs` code-behind, `BaseComponent` inheritance — no new DI, no new abstractions.
- Line VMs with populated `UoMName`: `PurchaseOrderLineVM`, `TransferOrderLineVM`, `TOxItemFulfillmentLineVM`, `InventoryCountingLineVM` (via `TransactionVM.cs:34`); `InventoryWorksheetLineVM` (via `InventoryItemVM.cs:16`) — note `InventoryWorksheetLineVM : InventoryItemVM` already serializes `UoMName`/`UoMRate` in the save payload as `""`/`0` today, so populating them changes values, not shape.
- Source for Worksheet scans: `/Item/PerBarcode` → `InventoryItemVM.UoMName` (fed by `NS_Item_Get_PerBarcode.sql:8`).
- Worksheet anchors (current, re-verified 2026-10-14): `ProcessScannedItem` initializer `:457-464`; `AddManualItem` initializer `:508-515`; `AllScannedItems` projection `:78-84`; `ScannedItemSummaryVM` `:880-888`; `SaveScan` posts at `:207-212`; `ScannedItemSummaryVM` has no `UoMName`; `TransactionVM.UoMRate` is `decimal` but the worksheet copies use `InventoryItemVM.UoMRate` (`int`) — same as math already reads at `:469`.
- Reference markup (Packing): `<RadzenText Text="@row.UoMName" TextStyle="TextStyle.Subtitle2" Style="font-size:.6rem" />`
- Build: `dotnet build` from solution root (`.claude/building_the_project.md`); probe shell with `echo ok` first per `CLAUDE.md`.
- No mobile UI test project exists — verification is build + diff + on-device.

**Global constraints:**
- Touch only `Mobile.MAUI/Components/Pages/{Receiving,InventoryCounting,Packing}/**` (C6).
- Do not modify/delete commented code (P3); do not touch save payloads, quantity math, search, tabs (P1/P2); do not remediate the `TransactionVM`/`InventoryItemVM.UoMName` shadowing debt.
- Keep each screen's field order; insert the UoM line after the last existing info line (D3); render empties as-is (D4).

## Phase 0 — Setup & baseline

- [ ] **T0.1** Create working branch `claude/feat/mobile-packaging-uom-item-cards` from current HEAD; confirm `git status` is clean apart from pre-existing untracked docs.
  - _Completion: branch checked out; baseline recorded._
  - _Requirements: P1–P4 (safety)_
- [ ] **T0.2** Re-verify the exact template line numbers below (code may have shifted since audit); update anchors before editing.
  - _Completion: all 11 insertion points + 3 C# sites confirmed present._
  - _Requirements: 1.2, 2.1_

## Phase 1 — Receiving card templates (data already in VM)

- [ ] **T1.1** `Mobile.MAUI/Components/Pages/Receiving/PurchaseOrder/PurchaseOrderItemView.razor` — insert the UoM line after the weight line in the Good card (`~:123-125`) and Bad card (`~:189-191`).
  - Current → target: info stack `Material / Vendor / Weight` → `Material / Vendor / Weight / UoMName`.
  - _Completion: C1 visible on both tabs on-device; search/tab behavior unchanged._
  - _Requirements: 1.1, 1.2, 1.3_
- [ ] **T1.2** `Mobile.MAUI/Components/Pages/Receiving/TransferOrder/TransferOrderItemView.razor` — same insertion in Good (`~:58`) and Bad (`~:89`) cards after the `{row.ScannedWeight}kg` line.
  - _Completion: C1 on both tabs; C2 ordering verified._
  - _Requirements: 1.1, 1.2, 1.3_
- [ ] **T1.3** `Mobile.MAUI/Components/Pages/Receiving/TransferOrder/TOxItemFulfillmentItemView.razor` — same insertion in Good (`~:92`) and Bad (`~:123`) cards.
  - _Completion: C1 on both tabs._
  - _Requirements: 1.1, 1.2, 1.3_
- [ ] **T1.4** `Mobile.MAUI/Components/Pages/Receiving/Returns/TOxReturnxItemFulfillmentItemView.razor` — same insertion in the single card (`~:87`).
  - _Completion: C1 on the Returns×IF list._
  - _Requirements: 1.1, 1.2, 1.3_
- [ ] **Checkpoint C-A:** `git diff --stat` shows only the 4 Receiving `.razor` files; `dotnet build` succeeds, zero errors.
  - _Requirements: 3.2, P1_

## Phase 2 — Reference consistency (D1)

- [ ] **T2.1** `Mobile.MAUI/Components/Pages/Packing/TransferOrder/TransferOrderItemView.razor` — insert the UoM line in the Bad-tab card (`~:122-124`, after `LocationName`, before `Available Qty`), matching its Good (`:92`) and BackOrdered (`:157`) siblings.
  - _Completion: all three Packing TO lists show UoM; packing scan/save untouched._
  - _Requirements: 1.1 (reference parity), P1_

## Phase 3 — Worksheet templates + plumbing (Mobile-only)

- [ ] **T3.1** `.../InventoryWorksheet/CreateInventoryWorksheetView.razor.cs` — in `ProcessScannedItem` (`:457-464`), add `UoMName = barcode.UoMName,` and `UoMRate = barcode.UoMRate,` to the `new InventoryWorksheetLineVM` initializer.
  - _Completion: C3 (scan) — scanned line carries UoM; payload shape unchanged (fields already serialized as `""`/`0`)._
  - _Requirements: 2.2, P2_
- [ ] **T3.2** Same file — in `AddManualItem` (`:508-515`), add `UoMName = ManualItem.UoMName,` and `UoMRate = ManualItem.UoMRate,` to the `new InventoryWorksheetLineVM` initializer.
  - _Completion: C3 (manual) — manual-entry lines carry UoM; without this, manual lines render a blank UoM line._
  - _Requirements: 2.3, P2_
- [ ] **T3.3** Same file — add `public string? UoMName { get; set; }` to `ScannedItemSummaryVM` (`:880-888`) and `UoMName = group.FirstOrDefault()?.UoMName,` in the `AllScannedItems` projection (`:78-84`). Use `NetsuiteMaterialInternalId` grouping already in place (`:81`); first-seen UoM per material (D2, accepted, including cross-bin first-seen).
  - _Completion: C4 — summary carries first-seen UoM._
  - _Requirements: 2.4_
- [ ] **T3.4** `.../CreateInventoryWorksheetView.razor` — insert the UoM line in the summary card (`~:89-95`), Good card (`~:133-148`), and Bad card (`~:175-190`) info stacks.
  - _Completion: C1 on all three worksheet lists._
  - _Requirements: 2.1_
- [ ] **Checkpoint C-B:** manual-input spec behaviors (scan, Move, Remove, guards, save) spot-checked unchanged on-device; verify the `/InventoryCounting/Worksheet/SaveScan` backend (code-read only, no change) ignores `UoMName`/`UoMRate` so populated payload values cannot alter posting behavior (C5).
  - _Requirements: 2.5, P1, P2_

## Phase 4 — Verification & close-out

- [ ] **T4.1** Build: shell probe (`echo ok`) → `dotnet build` from solution root; zero errors (C7). If the shell probe fails, stop and report per `CLAUDE.md` (do not loop).
  - _Requirements: C7_
- [ ] **T4.2** Diff audit: `git status`/`git diff --stat` shows only the 6 `.razor` + 1 `.razor.cs` files (C6); commented code intact (P3).
- [ ] **T4.3** On-device pass: PO / Receiving TO / TO×IF / Returns×IF (Good+Bad), Worksheet (scan → Good/Bad → All summary → save; manual-entry → Good/Bad), spot-check Counting + Receiving Returns (unchanged), Packing TO Bad tab.
  - _Completion: verification checklist in design (C1–C5) all ticked._
- [ ] **T4.4** Commit per `CLAUDE.md` workflow (short imperative message + co-author trailer) on the `claude/` branch; report paths and any verification limitation. Do **not** write `implementation-notes.md` (only on request, post-implementation).
  - _Requirements: 3.3_

**Traceability note:** every task above carries `_Requirements_`; C1–C7 map to §Correctness properties in `design.md`. No task touches Web/API/DTO/Mapster/SQL/NetSuite, tests, or architectural debts.
