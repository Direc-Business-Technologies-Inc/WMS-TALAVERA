---
Date: 2026-10-08
Status: Confirmed
Mode: BUGFIX
Spec: design.md (this folder)
Goal: Barcode-created STR/RTS lines default their Preferred Vendor from the item vendor master, matching the picker path.
---

# Implementation Plan

## Global Constraints

- **Do not build** — the user is running the project live; changes are hot-reload friendly but the integration layer requires an app restart to take effect (no `dotnet build` in this session).
- Additive only: no form code-behind edits, no `SuiteQLFragments` signature changes, no changes to `ItemsIntegration`, `BarcodeDTO`, `BarcodeVM`, `ItemsDTO`, `ItemsVM`, `BarcodeStore`, or any verifier.
- Architectural debts (`.claude/architectural_debts.md`) untouched.
- Handler-dispatch pattern stays as-is (Debt 5 is intentional).
- No test projects exist; verification is manual DEV matrix per design.

## Phase A — Integration Query (root cause)

**Task 1: Add vendor fields to `BarcodeNSDTO`**
- File: `Integration.NS/DataTransferObjects/Receiving/BarcodeNSDTO.cs` (after `:20` `PreferredBin`)
- Add:
  ```csharp
  public int? PreferredVendorId { get; set; }
  public string? PreferredVendorName { get; set; }
  ```
- `_Requirements: 1.2, 2.1_`

**Task 2: Extend `GetBarcodeData` SuiteQL**
- File: `Integration.NS/Implementations/Transactions/ReceivingIntegration.cs:742-767`
- 2a. In the `.Select(...)` block (after the `PreferredBin` select at `:756`), add:
  ```csharp
  ("pv.vendor", nameof(BarcodeNSDTO.PreferredVendorId)),
  ("BUILTIN.DF(pv.vendor)", nameof(BarcodeNSDTO.PreferredVendorName)),
  ```
- 2b. After the `PreferredBin` LeftJoin at `:763`, add:
  ```csharp
  .LeftJoin($"{SuiteQLFragments.PreferredVendor()} pv",
      on: "pv.item = item.id AND pv.subsidiary = loc.subsidiary")
  ```
  Unscoped fragment call + per-row subsidiary predicate — avoids prior-spec regression S1 (row duplication). Do NOT add a subsidiary parameter to the method.
- Do not touch the `WithFilters` block, the `PreferredBin` join, or any other query in the file.
- `_Requirements: 1.2, 2.1, 2.2, 3.2_`

**Task 3: Map fields into `BarcodeDTO.Item`**
- File: `Integration.NS/Implementations/Transactions/ReceivingIntegration.cs:773-784`
- In the nested `Item = new() { ... }` initializer, after `PreferredBin = barcodeData.PreferredBin,` add:
  ```csharp
  PreferredVendorId = barcodeData.PreferredVendorId,
  PreferredVendorName = barcodeData.PreferredVendorName,
  ```
- No Mapster registration needed: `ItemsDTO → ItemsVM` maps by name (fields already exist at `ItemsVM.cs:15-16`).
- `_Requirements: 2.1_`

## Phase B — Verification (manual, DEV)

**Task 4: Demonstrate defect is fixed (design test 1-2)**
- Scan an item with a known preferred vendor into a NEW STR line via barcode → line vendor auto-fills, equal to the picker path's default for the same item + location.
- Repeat on Supplier Return (RTS) form.
- `_Requirements: 2.1_`

**Task 5: No-vendor + precedence checks (design test 3-4)**
- Scan an item with no preferred vendor in the location's subsidiary → line vendor null → submit blocked with toast naming the line.
- RTS with header vendor set → scan → header vendor wins on the new line.
- `_Requirements: 2.2, 2.3_`

**Task 6: Preservation sweep (design test 5)**
- Picker-path add still defaults (`ItemsIntegration` untouched — P5).
- Preferred bin, quantity, UoM still resolve on scan (P3/P6).
- One non-vendor consumer (e.g., Receiving scan) behaves as before (P6).
- `_Requirements: 3.1, 3.2, 3.4_`

**Checkpoint:** after Phase A, restart the app host (integration code is not hot-reloaded), then run Tasks 4-6. All three pass → fix complete; Tasks 1-3 trace to requirements 1.2/2.1/2.2/2.3/3.2; Tasks 4-6 verify 2.1/2.2/2.3/3.1/3.2/3.4.

## Optional (separate from MVP)

- **Optional Task 7:** If DEV reveals an item with multiple `preferredvendor='T'` rows in one subsidiary (R2), aggregate the fragment with `MIN(vendor)` mirroring `PreferredBin()`'s `MIN` pattern — only on evidence, not preemptively.
