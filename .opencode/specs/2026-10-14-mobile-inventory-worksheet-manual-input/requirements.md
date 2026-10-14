# Requirements — Mobile Inventory Worksheet Manual Input

**Date:** 2026-10-14
**Status:** Confirmed
**Mode:** FEATURE
**Scope:** `Mobile.MAUI` — manual item entry on the existing Inventory Worksheet scan page (`/inventorycounting/worksheet`). Single bin per scan session (confirmed with business).
**Related:** None found

## Introduction

Warehouse staff currently populate the mobile Inventory Worksheet exclusively by scanning barcodes. When barcodes are unreadable, missing, or items are counted without physical labels, there is no way to record quantities. This feature adds manual item entry to the existing mobile worksheet page, reusing the same line model, save pipeline, and Good/Bad semantics as scanning. Confirmed constraint: **single bin per scan session** — the globally selected bin applies to all lines, manual or scanned.

Note: `Web.BlazorServer` is referenced for parity and regression comparison only; no files in that project are modified.

## Glossary

- **Worksheet line** — one item + bin + Good/Bad quantities entry in `ICItems`.
- **Good / Bad** — inventory status of counted quantity (NetSuite inventory status 1 / 3).
- **Manual entry** — adding quantity to a worksheet line by selecting an item and typing a quantity, without scanning.
- **Bin context** — the currently selected bin (`CurrentBinInternalId`), or 0 when the location has no bins.

## Requirements

### 1. Manual item entry

- **1.1** User story: *As a warehouse counter, I can select an item from a searchable list and type a quantity, so I can record counts for items I cannot scan.*
- **1.2** WHEN the user opens manual entry, THEN the system SHALL present a searchable item dropdown populated from `GET /InventoryCounting/Worksheet/Items`.
- **1.3** WHEN the user submits a manual entry with a valid item and quantity > 0, THEN the system SHALL add or update the worksheet line for that item in the current bin context, applying the quantity to Good or Bad per the active Good/Bad toggle.
- **1.4** WHEN the manual item already exists as a line in the current bin context, THEN the system SHALL increment that line's quantity (not create a duplicate line).
- **1.5** WHEN the manual entry is submitted, THEN the system SHALL clear the item selection and quantity input for the next entry.

### 2. Entry guards (parity with scan flow)

- **2.1** WHEN manual entry is attempted with no location selected, THEN the system SHALL show a warning and NOT modify any line.
- **2.2** WHEN the selected location has bins and no bin is selected, THEN the system SHALL show a warning and NOT modify any line.
- **2.3** WHEN manual entry is attempted with no item selected or quantity ≤ 0, THEN the system SHALL show a warning and NOT modify any line.

### 3. Save validation (parity with web)

- **3.1** WHEN saving, THEN the system SHALL reject lines with negative Good or Bad quantities.
- **3.2** WHEN saving, THEN the system SHALL reject the save when no location is selected, when a bin is required but not selected, or when there are no counted lines (existing `SaveScan` guards preserved).

### 4. Location change safety

- **4.1** WHEN the user changes location while `ICItems` is non-empty, THEN the system SHALL ask for confirmation and, on confirm, clear all worksheet lines (matching web's `ClearForm` behavior); on cancel, the location SHALL NOT change.
- **4.2** WHEN the user changes location while `ICItems` is empty, THEN the system SHALL clear lines and bins without confirmation (current behavior).

### 5. UI behavior

- **5.1** WHEN a manual entry is added, THEN the system SHALL reflect it immediately in the visible list (current tab view or All summary).
- **5.2** Manual entry SHALL be disabled while a save or lookup action is busy (same disabled-state pattern as other scan actions).

## In scope

- Mobile `CreateInventoryWorksheetView` only: item-list loading, manual-add UI, guards, save validation, location-change confirmation.

## Out of scope

- Web Blazor worksheet page changes.
- API/backend changes (`Worksheet/Items`, `Worksheet/SaveScan`, payload builder are reused as-is).
- Multi-bin allocation per item (confirmed not needed).
- Branching/commit workflow.
- Automated tests for the mobile client (deferred to a later effort by decision; see verification approach in design).

## Preservation requirements

- **P1** Scan flow behavior (Good/Bad toggle, Move, Remove, scan lock) SHALL NOT change.
- **P2** Save payload shape (`InventoryCountItems` / `Location` / `NetsuiteUserSubsidiaryInternalId` → `PostInventoryWorksheetCmd`) SHALL NOT change.
- **P3** Existing no-bin location flow (info alert, bin 0) SHALL NOT change.
- **P4** Existing commented code blocks in `CreateInventoryWorksheetView.razor.cs` SHALL NOT be modified or deleted (may be needed later).
