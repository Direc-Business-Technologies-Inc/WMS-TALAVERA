---
Date: 2026-10-06
Status: Confirmed
Mode: BUGFIX
Spec: design.md (same folder)
Related: None found
---

# Implementation Plan: Preferred Vendor per Line (STR + RTS)

## Goal

Required, editable Preferred Vendor per item line on STR (TO/ITO/Return) and
RTS, defaulted from the item vendor master, posted to `custcol_dbti_vendor`;
STR header vendor restricted to Return categories.

## Verified Technical Context

- .NET 8, Blazor Server, Radzen, MediatR, Mapster; build: `dotnet build`
  (`.claude/building_the_project.md:3-6`). **No test projects exist** — no
  automated tests are added; verification is build + manual matrix (see Phase 5).
- Branch convention `claude/fix/<issue-log-number>` (CLAUDE.md:32-33). Current
  branch `claude/fix/packing-label-exempt-and-non-exempt` holds unrelated
  uncommitted STR work — reconcile per Task 1 before implementing.
- Conventions: handler dispatch stays in `Web.BlazorServer/Handlers` (Debt 5);
  validation in code-behind; Mapster maps by name at NSDTO→DTO and DTO→VM
  seams; DTO/VM nullable style per `string? PreferredBin` precedent.

## Tasks

### Phase 0 — Prerequisites & Working-Tree Reconciliation

- [ ] 1. Reconcile the uncommitted header-vendor work with this spec
  - Keep in `STRForm.razor.cs`: `VendorSubsidiary` (:221-228), `VendorProvider`
    with try/finally (:230-250), `VendorSet` (:252-256) — reused by the line
    dropdown (D2).
  - Change in `STRForm.razor` (:58-80): header vendor column becomes
    `Visible="@Model.IsReturn"`; validator visibility gated the same way.
  - Keep `StockTransferRequestIntegration.cs:376` (`custbody_dbti_return_to_vendor`,
    IsReturn-gated) as-is.
  - Supersede the `StockTransferRequestLineVM.cs` vendor removal: Task 10 re-adds
    it nullable.
  - Acceptance: header vendor visible only on Return categories; TO/ITO show no
    header field.
  - _Requirements: 2.7, 3.2_

- [ ] 2. External verifications (with NetSuite admin if needed)
  - Run the coverage probe for `transferorder` and `vendorreturnauthorization`
    lines (`custcol_dbti_vendor` present).
  - Confirm the NS defaulting script runs on VRA saves.
  - Confirm `custcol_dbti_created_vendor_returns` was NOT modified (R5).
  - Acceptance: all three confirmed, or discrepancies reported back before
    integration tasks proceed.
  - _Requirements: 2.4, 2.9; Risks R2, R5_

- [ ] 3. Checkpoint — prerequisites green
  - `dotnet build` succeeds; Task 2 answers recorded in the spec folder.

### Phase 1 — Read Path (Integration.NS)

- [ ] 4. STR line read: select the line vendor
  - File: `Integration.NS/Implementations/Transactions/StockTransferRequestIntegration.cs`
  - `GetStockTransferRequestLines` (:222-261): add selects
    `("tl.custcol_dbti_vendor", …VendorId)` and
    `("BUILTIN.DF(tl.custcol_dbti_vendor)", …VendorName)` — mirror the header
    pair at :158/:165.
  - `Integration.NS/DataTransferObjects/StockTransferRequest/StockTransferRequestLineNSDTO.cs`:
    add `VendorId`, `VendorName` (types mirror
    `StockTransferRequestHeaderNSDTO.cs:14-15` — verify exact types).
  - Map in the existing `Adapt` block (:254-260): `Vendor = …` only when id present.
  - Acceptance: opening an existing STR shows stored line vendors (verified later
    in Phase 5, item 2).
  - _Requirements: 2.6_

- [ ] 5. RTS line reads: select the line vendor
  - File: `Integration.NS/Implementations/Transactions/SupplierReturnIntegration.cs`
  - Add the same select pair to `GetReturnLinesAsync` (:100-129) and
    `GetReturnFromPurchaseOrderLinesAsync` (:307-336).
  - Extend `SupplierReturnLineNSDTO`; map in `ConvertLineDTO` (:433-440).
  - _Requirements: 2.2, 2.6_

- [ ] 6. Checkpoint — read path compiles
  - `dotnet build` succeeds.

### Phase 2 — Write Path (Integration.NS)

- [ ] 7. STR payload: post line vendor
  - `CreateSTRPayload` line items (:385-395): add
    `custcol_dbti_vendor = line.Vendor is not null ? new { id = line.Vendor.Id.ToString() } : null`
    (object shape per :376 precedent). No category gate. Same payload serves
    create (:279-296) and update (:298-314) → wipe protection (2.4) satisfied.
  - _Requirements: 2.1, 2.4, 2.5_

- [ ] 8. RTS payload: post line vendor
  - `CreatePayload` line items (:414-421): same addition.
  - Explicitly do NOT modify the PO transform (:236-258) — D5.
  - _Requirements: 2.2, 2.4, 2.5_

- [ ] 9. Checkpoint — write path compiles
  - `dotnet build` succeeds.

### Phase 3 — Models & Default Sourcing

- [ ] 10. Line DTOs and VMs
  - `StockTransferRequestLineDTO.cs`, `SupplierReturnLineDTO.cs`: add
    `VendorDTO? Vendor`.
  - `StockTransferRequestLineVM.cs`, `SupplierReturnLineVM.cs`: add
    `VendorVM? Vendor` (nullable — NOT `= new()`).
  - Investigation subtask: confirm Mapster maps `VendorDTO→VendorVM` on line
    collections by name (header already does); if not, register in
    `Application.UseCases/Registers/MappingRegistration.cs`.
  - _Requirements: 2.1, 2.2, 2.6_

- [ ] 11. PreferredVendor SuiteQL fragment
  - `Integration.NS/Helpers/SuiteQLFragments.cs`: add
    `PreferredVendor(...)` modeled on `PreferredBin()` (:41-52), derived table
    over `itemvendor WHERE preferredvendor = 'T'` scoped by subsidiary, SQL per
    `NSScripts/NS_TransferOrder_Get_Items.sql:69-80`. XML-doc the why.
  - _Requirements: 2.3_

- [ ] 12. Item picker default sourcing
  - `ItemsIntegration.GetItemsByLocationDataGridAsync` (:58-94): consume the
    Task-11 fragment; add selects for preferred vendor id/name.
  - Investigation subtask: identify the picker NSDTO/DTO/VM files (items grid
    models) and add the two fields.
  - `STRForm.razor.cs` line-add (:130-154) and `SupplierReturnForm.razor.cs`
    (:283-308): set `line.Vendor` from picker values (null when absent).
  - _Requirements: 2.3_

- [ ] 13. Checkpoint — models compile
  - `dotnet build` succeeds; new picker fields flow to line add.

### Phase 4 — UI

- [ ] 14. STR lines grid: vendor column + validation
  - `STRForm.razor`: add column after the UoM column (:219-228 precedent):
    `RadzenText` (read mode, `data.Vendor?.Name ?? "Not set"`) +
    `QuickVirtualizedDropdown` (edit mode, `TItem="VendorVM"`,
    `DataGetter="@VendorProvider"`, `@bind-Value:get="@data.Vendor"`,
    `@bind-Value:set="@(val => LineVendorSet(data, val))"`,
    `AllowClear=false`, `AllowFiltering`).
  - `STRForm.razor.cs`: add `LineVendorSet` (mirror `SetLineUoM` :620-628);
    extend the submit guard near :113-120: collect `LineNumber`s where
    `Vendor is null`; if any, `ToastService.Error` naming them and return.
  - _Requirements: 2.1, 2.5, 2.6, 2.8_

- [ ] 15. RTS lines grid: vendor column + validation
  - `SupplierReturnForm.razor`: same column pattern after the UoM column
    (:118-125), reusing the existing header `VendorProvider`.
  - `SupplierReturnForm.razor.cs`: `LineVendorSet` + the same line-numbered
    submit guard on its submit path.
  - _Requirements: 2.2, 2.5, 2.6, 2.8_

- [ ] 16. Checkpoint — UI compiles and renders
  - `dotnet build` succeeds; both grids render the column in all modes.

### Phase 5 — Manual Verification (DEV/TC)

- [ ] 17. Fix-checking matrix
  - Create/edit/view per type: TO, ITO, Return-Good, Return-Bad, RTS,
    RTS-from-PO → column present, dropdown works, default applied (P1, P2).
  - Override persistence: non-default vendor → save → verify in NetSuite the
    override survived (P9); re-open in WMS shows it (P5).
  - Update round-trip: edit doc, touch nothing else, save → vendors retained
    (P3).
  - Validation: line without default → submit blocked, toast names the line (P7).
  - Header: hidden on TO/ITO; visible + required on Returns (P6).
  - _Requirements: 2.1-2.8_

- [ ] 18. Preservation checks
  - New Return posts `custbody_dbti_return_to_vendor`; Receiving list shows it (3.2).
  - RTS create still requires/posts header `entity` (3.1).
  - Packing/receiving item queries unchanged — smoke one packing fetch (3.3).
  - _Requirements: 3.1-3.4_

- [ ] 19. Final checkpoint
  - Full `dotnet build`; matrix results recorded; spec documents updated to
    Status: Confirmed for tasks.

## Notes

- No Optional tasks — everything above is MVP.
- Tasks 4-8 require the confirmed field ID (done: `custcol_dbti_vendor`) and
  should land after Task 2's verifications.
- Do not create branches/commits as part of this plan; branch per CLAUDE.md
  convention when implementation starts.
