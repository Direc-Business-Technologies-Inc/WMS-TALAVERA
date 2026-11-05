# Requirements — Mobile Packaging UoM on Item Cards (Receiving, Counting, Worksheet)

**Date:** 2026-10-14
**Status:** Confirmed
**Mode:** FEATURE
**Scope:** `Mobile.MAUI` item-card templates under `Components/Pages/Receiving/**` and `Components/Pages/InventoryCounting/**`, plus two Mobile-only data-plumbing fixes in `CreateInventoryWorksheetView.razor.cs`. No `Web.BlazorServer`, API, DTO, SQL, or NetSuite changes. Packing pages are the read-only reference pattern and are not modified — except one noted reference inconsistency (decision D1, confirmed in scope).
**Related:** `2026-10-14-mobile-inventory-worksheet-manual-input` (touches the same worksheet file; this spec only adds display lines and must not alter that spec's scan/manual behaviors), `2026-10-08-mobile-po-item-receipt-line-selection` (same PO file; posting path untouched).

## Introduction

Mobile Packing item cards display the transaction line's packaging UoM (`UoMName`) beneath the material name, but the Receiving, Counting, and Worksheet item cards omit it — even though every one of them already holds the value in its line VM (populated at `.razor.cs` load: PO `:117,159`; Receiving TO `:91,129`; TO×IF `:116,154`; Returns×IF `:105,534,585`; Counting `:75,102,351`). Warehouse staff therefore cannot see the UoM on the cards they scan against. This feature renders the existing `UoMName` on every in-scope item card, matching the Packing pattern. The sole exceptions requiring data work are in the Worksheet: its scan handler (`ProcessScannedItem`, `.razor.cs:457-464`) and manual-entry handler (`AddManualItem`, `.razor.cs:508-515`) both drop `UoMName` when building lines, and its summary VM (`ScannedItemSummaryVM`, `.razor.cs:880-888`) has no UoM field — all fixed in Mobile code only.

**Coverage confirmation (audited 2026-10-14):** every `Template Context="row"` in `Mobile.MAUI/Components/Pages/**` was enumerated. All transaction item cards are either already compliant (Counting, Receiving Returns, Packing reference) or in scope below. Trip Ticket's cards (`CreateTripTicketView.razor:26,53`) are transaction-level (order number + document date), not item cards — no line UoM exists to display. The requirement "Packaging UoM visibility for all item cards in all transactions, like in packing" is therefore fully satisfied by this spec's scope.

## Glossary

- **Item card** — one row template (`RadzenDataList` → `Template Context="row"`) on a Mobile transaction items page.
- **Packaging UoM** — the transaction line's `UoMName` (line UoM, e.g. the NetSuite `tl.units → unitname` value shown in Packing); display-only, no quantity math changes.
- **Last-Scanned banner** — the floating `LastScanned` panel at the top of Receiving pages; already shows `UoMName` where present and is not modified.

## Requirements

### 1. Receiving item cards show Packaging UoM

- **1.1** WHEN a Receiving PO / Receiving TO / TO×ItemFulfillment / Returns×ItemFulfillment item card renders, THEN it SHALL display `row.UoMName` in the info stack beneath the material name, in the same `Subtitle2 / .6rem` style as Packing.
- **1.2** The UoM line SHALL appear on both Good and Bad tabs wherever both exist (PO Good `:113-127` / Bad `:179-193`; Receiving TO Good `:55-59` / Bad `:86-90`; TO×IF Good `:89-93` / Bad `:120-124`; Returns×IF single card `:84-88`).
- **1.3** The UoM line SHALL be placed after the last existing info line (weight/vendor/location), keeping each screen's current field order — matching Packing's Material → context → UoM ordering.

### 2. Worksheet cards show Packaging UoM (with Mobile-only plumbing)

- **2.1** WHEN a Worksheet summary / Good / Bad card renders, THEN it SHALL display the line's UoM in the info stack (`CreateInventoryWorksheetView.razor` summary `:84-96`, Good `:132-149`, Bad `:174-191`).
- **2.2** WHEN a scan creates an `InventoryWorksheetLineVM`, THEN `ProcessScannedItem` SHALL copy `UoMName` (and `UoMRate`) from the scanned `InventoryItemVM` (currently dropped at `.razor.cs:457-464`).
- **2.3** WHEN a manual entry creates an `InventoryWorksheetLineVM`, THEN `AddManualItem` SHALL copy `UoMName` (and `UoMRate`) from `ManualItem` (currently dropped at `.razor.cs:508-515`). Without this, manual-entry lines would render a blank UoM line.
- **2.4** The `ScannedItemSummaryVM` SHALL gain a `UoMName` property, populated in the `AllScannedItems` projection (`.razor.cs:78-84`) from the group's first line.
- **2.5** `SaveScan` posts `ICItems` directly (`.razor.cs:207-212`); `InventoryWorksheetLineVM` already inherits `UoMName`/`UoMRate` from `InventoryItemVM`, so the payload **shape** is unchanged. The payload **values** go from `""`/`0` to populated — the `/InventoryCounting/Worksheet/SaveScan` backend SHALL be confirmed to ignore these fields (verification only; backend is out of scope).

### 3. Out-of-scope confirmations

- **3.1** Counting item view (`InventoryCountingItemView.razor:27,56`) and Receiving Returns (`ReturnsItemView.razor:56`) already show `UoMName` — verified compliant, no change.
- **3.2** Web app, API controllers, DTOs, Mapster profiles, SQL/SuiteQL, and NetSuite integrations SHALL NOT be modified.
- **3.3** No quantity math, scan/save payloads, search behavior, or tab filtering SHALL change — display-only.

## In scope

- 5 Receiving/Worksheet `.razor` card templates + `CreateInventoryWorksheetView.razor.cs` plumbing (3 fixes: scan, manual-entry, summary).
- Packing TO Bad-tab +1 line (decision D1, confirmed).

## Out of scope

- Packing pages otherwise (reference only), Trip Ticket (no item cards), Last-Scanned banners, Web, backend, tests for the mobile client (no mobile test project exists; verification is on-device/build per repo practice).

## Preservation requirements

- **P1** Card selection, scan/save actions, and Good/Bad tab behavior SHALL NOT change.
- **P2** `InventoryWorksheetLineVM` save payload **shape** SHALL NOT change — `UoMName`/`UoMRate` are already inherited from `InventoryItemVM` and serialized today as `""`/`0`; populating them only changes values (backend fields-ignored confirmation per 2.5).
- **P3** Existing commented code blocks SHALL NOT be modified or deleted.
- **P4** No architectural-debt remediation (per `.claude/architectural_debts.md`); no new cross-layer patterns — Razor + existing VM properties only.

## Decisions (confirmed)

- **D1** Packing TO Bad tab (`TransferOrderItemView.razor:121-124`) lacks `UoMName` while its Good/BackOrdered siblings have it — **confirmed in scope** (+1 line).
- **D2** Worksheet summary groups by material across bins; UoM projects first-seen value — **accepted**; multi-UoM same-material scans are an edge case (compounded cross-bin: lines key material+bin, summary keys material only — first bin's UoM wins). Note this is compounded across bins: the summary groups by material only (`.razor.cs:81`) while lines are keyed by material+bin (`.razor.cs:448-450`), so the same material seen in two bins with different UoMs shows the first bin's UoM on the summary.
- **D3** UoM line placed after the last existing info line per card, keeping each screen's field order.
- **D4** Empty `UoMName` renders as-is, like Packing (no conditional markup).
