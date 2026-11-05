---
Date: 2026-10-08
Status: Confirmed
Mode: BUGFIX
Scope: Preferred Vendor defaulting on lines added via barcode scanning (STR + RTS/Supplier Return web forms)
Related: .opencode/specs/2026-10-06-preferred-vendor-per-line/ (requirements.md, design.md — Confirmed)
---

# Bugfix Requirements Document

## Introduction

The confirmed preferred-vendor-per-line fix defaults each new STR/RTS line's
Preferred Vendor from the NetSuite item vendor master. That defaulting works
when lines are added via the item picker, but not when lines are created by
barcode scanning, because the barcode lookup query never fetches the item's
preferred vendor. Users scanning barcodes must manually set the vendor on every
line — inconsistent with the picker path and prone to submit-blocking errors.

## Glossary

- **Barcode path**: line creation via `BarcodeScannerDialog` → `GetBarcodeData` → `ApplyBarcodes`, as opposed to the item picker.
- **Preferred Vendor (master)**: vendor flagged `preferredvendor = 'T'` on an item's vendor list, subsidiary-scoped (per prior spec).
- **Owning subsidiary**: the subsidiary of the scanned location (`loc.subsidiary`) for the barcode path.
- **Preservation**: existing behavior that must not regress.

## Bug Analysis

### Current Behavior (Defect)

1.1 WHEN a user scans a barcode that creates a NEW STR or RTS line (no existing
line for that item), THEN the line's Preferred Vendor is empty — even though the
same item added via the item picker defaults correctly
(`STRForm.razor.cs:149` vs `:490`; `SupplierReturnForm.razor.cs:329` vs `:412`).

1.2 WHEN the barcode lookup runs, THEN `GetBarcodeData`
(`ReceivingIntegration.cs:742-767`) selects preferred bin but no preferred
vendor fields, and `BarcodeNSDTO` carries no vendor properties — so
`barcode.Item.PreferredVendorId` is always null regardless of master data.

1.3 WHEN a scanned item has a preferred vendor and the user submits, THEN the
submit guard (`STRForm.razor.cs:120-124`) blocks with "Preferred Vendor is
required on line(s): …" — a manual workaround, not a default.

### Expected Behavior (Correct)

2.1 WHEN a barcode scan creates a new STR or RTS line, THEN the line's Preferred
Vendor SHALL default to the item's Preferred Vendor (master) scoped to the
scanned location's subsidiary — identical to the picker-path default (prior
spec req 2.3).

2.2 WHEN the scanned item has no preferred vendor in that subsidiary, THEN the
line vendor SHALL remain null and existing submit validation SHALL apply
unchanged (prior spec req 2.8).

2.3 WHEN the user has pre-selected a line (or the document has a header vendor
for RTS), THEN existing precedence SHALL be preserved: header vendor wins,
item-master default is the fallback (`SupplierReturnForm.razor.cs:411-420`).

### Unchanged Behavior (Preservation)

3.1 WHEN the item picker adds lines, THEN its defaulting SHALL behave exactly as
today (`ItemsIntegration.cs:88-98` untouched).

3.2 WHEN `GetBarcodeData` resolves preferred bins, quantities, or UoM, THEN
those fields SHALL behave byte-identically to before.

3.3 WHEN barcodes are cached in `BarcodeStore` or verified by `IsValidBarcode`,
THEN no behavior changes.

3.4 WHEN other barcode-consuming forms without a vendor field run (Receiving,
ITR, Inventory Adjustment, Packing, Counting), THEN they SHALL be unaffected —
the added join/selects are inert for them.

3.5 Documented architectural debts SHALL NOT be fixed or refactored.

## Scope

### In-Scope
- `GetBarcodeData` SuiteQL: `PreferredVendor` join + two selects, subsidiary-scoped via `loc.subsidiary`.
- `BarcodeNSDTO`: `PreferredVendorId` / `PreferredVendorName`.
- Mapping into `BarcodeDTO.Item` (nested `ItemsDTO`).

### Out-of-Scope
- Form code-behind changes (already correct).
- `SuiteQLFragments.PreferredVendor` signature changes.
- Mobile (MAUI), NS scripts, item picker query.
- Automated test projects (none exist).

## Risks

- **R1**: Joining `PreferredVendor()` unscoped would duplicate rows (prior spec S1). Mitigated by constraining `pv.subsidiary = loc.subsidiary` in the join predicate.
- **R2**: An item with multiple `preferredvendor='T'` rows in one subsidiary could still multiply rows — same accepted risk as the picker path (prior spec D3).
