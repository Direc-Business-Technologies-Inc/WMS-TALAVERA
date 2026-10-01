# IIL_258: Vendor Search on Return to Supplier

- **Date:** 2026-10-01
- **Status:** Confirmed
- **Mode:** BUGFIX
- **Scope:** Vendor search on the Return to Supplier (RTS) create form
- **Related:** None found
- **Spec:** [design.md](design.md)

## Goal

Make the RTS Vendor dropdown search by vendor reference or vendor name while preserving existing paging, virtualization, selection, and dependent form behavior.

## Architecture Summary

The RTS form passes a `DataGridIntent` through `VendorProvider` and the existing `VendorHandler` / `GetVendorsListQry` path to `Integration.NS`. Search is represented as an OR filter on vendor reference and name, then mapped by the existing NetSuite query builder.

## Verified Technical Context

- The vendor dropdown is in `Web.BlazorServer/Components/Pages/Transaction/SupplierReturn/Components/SupplierReturnForm.razor`.
- `VendorProvider` is in the adjacent `.razor.cs` file.
- The shared control adds a filter only when `FilterTarget` is configured.
- `VendorVM` contains `ReferenceNumber` and `Name`; the NetSuite query selects both.
- No automated test project was found. Use focused manual validation and the documented solution build; do not claim automated test coverage.

## Global Constraints

- Keep the UI-to-data path through existing handler interfaces and MediatR queries.
- Keep the fix scoped to RTS; do not change the shared dropdown API or other vendor selectors.
- Preserve paging, virtualization, display text, validation, vendor selection, and subsidiary behavior.
- Do not add schema, DTO, or ViewModel changes.
- Keep any necessary search-literal escaping local to the vendor query path; do not expand this issue into generic SuiteQL refactoring.
- Before implementation, resolve the contradictory branch timing instructions in `CLAUDE.md`. This plan does not create branches, commits, or pushes.

## Tasks

### Phase 1: Baseline Reproduction

1. Reproduce the current behavior on the RTS create form by searching for a known vendor code and a known vendor name. Record whether results remain unfiltered. _Requirements: 1.1, 2.1, 2.2_
   - **Files:** No source edits; exercise the RTS create route.
   - **Completion criteria:** The reported failure is observed before the fix, or the environment limitation is recorded.

### Phase 2: Implement the Search Filter

2. Configure the Vendor dropdown in [SupplierReturnForm.razor](../../../Web.BlazorServer/Components/Pages/Transaction/SupplierReturn/Components/SupplierReturnForm.razor#L44) with a `FilterTarget` for `VendorVM.ReferenceNumber`. _Requirements: 2.1, 2.3_
   - **Current:** The dropdown sets `TextProperty` but omits `FilterTarget`.
   - **Target:** Typed search text is included in the data intent using the vendor reference property as the filter entry point.
   - **Completion criteria:** A non-empty search produces a filter intent and blank search preserves the unfiltered initial request.

3. Update `VendorProvider` in [SupplierReturnForm.razor.cs](../../../Web.BlazorServer/Components/Pages/Transaction/SupplierReturn/Components/SupplierReturnForm.razor.cs#L125) to replace the generated single-field filter with an OR group of contains filters for `VendorVM.ReferenceNumber` and `VendorVM.Name`. Preserve paging and any non-search filters. _Requirements: 2.1, 2.2, 2.3_
   - **Current:** The provider forwards the intent to `GetVendorsListAsync` without broadening the search across both fields.
   - **Target:** A vendor is returned when either its reference or its name contains the entered text.
   - **Completion criteria:** Inspect the intent to verify OR semantics, property names, and unchanged paging values.

4. Verify the NetSuite filter mapping and ensure user-entered apostrophes are treated as literal search text in the vendor query path. Make any required narrow change in [VendorIntegration.cs](../../../Integration.NS/Implementations/Others/VendorIntegration.cs#L22); use the existing alias mapping established by [SuiteQLQueryBuilder.cs](../../../Integration.NS/Services/SuiteQLQueryBuilder.cs#L293). Do not modify the generic query builder unless implementation evidence shows the local approach cannot meet the requirement. _Requirements: 2.1, 2.2_
   - **Current:** The query builder interpolates `Contains` text into SuiteQL.
   - **Target:** Both vendor properties map to their selected NetSuite columns, and search text does not break the query.
   - **Completion criteria:** Verify generated filters target the reference and name columns and apostrophe input is safely represented.

### Phase 3: Regression Validation

5. Validate the RTS form against development NetSuite data: partial/full vendor code, partial vendor name, no matches, blank search, paging/virtualization, apostrophe input, and vendor selection. Confirm subsidiary selection, required-field validation, and dependent behavior are unchanged. _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_
   - **Files:** No additional changes expected unless validation finds a defect in the touched slice.
   - **Completion criteria:** All listed checks pass, or unavailable environment/data is reported explicitly.

6. Verify the shell using the repository-prescribed probe, then run `dotnet build` from the solution root. After a successful build, follow the repository's resolved branch, staging, commit, and push workflow. _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_
   - **Completion criteria:** The shell probe succeeds, the solution builds successfully, and the applicable repository workflow is followed. No automated test result is claimed because no test project was found.

## Optional Work

None. Adding a test project or performing generic SuiteQL hardening is outside this issue's confirmed scope.
