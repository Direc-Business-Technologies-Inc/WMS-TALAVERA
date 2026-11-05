# STR UoM Dropdown Search Fix — Implementation Notes

- **Date:** 2026-10-27
- **Status:** Complete
- **Mode:** Bugfix implementation notes
- **Scope:** STR line UoM filtering while creating a transfer order.
- **Related:** [Root-cause analysis](../2026-10-26-str-uom-dropdown-search-rca.md)

## Shipped Changes

- Added `FilterTarget="@nameof(ItemUnitVM.Name)"` to the STR line UoM `QuickVirtualizedDropdown` in `Web.BlazorServer/Components/Pages/Transaction/StockTransferRequest/Components/STRForm.razor`.
- Added a regression test in `Integration.NS.Tests/Services/SuiteQLUnresolvedPropertyGuardTests.cs` confirming that a grid `Contains` filter on the `Name` select alias resolves to `uom.unitname` and produces a case-insensitive `LIKE` predicate.

The dropdown now converts its entered search text into a `DataGridIntent` filter. The item-unit integration applies that intent through the SuiteQL builder, which resolves the DTO-facing `Name` alias to the selected `uom.unitname` expression. The existing item-specific filter remains in place.

## Verification

- `dotnet test Integration.NS.Tests/Integration.NS.Tests.csproj --filter FullyQualifiedName~SuiteQLUnresolvedPropertyGuardTests` — passed, 8 tests.
- `dotnet build Web.BlazorServer/Web.BlazorServer.csproj --no-restore -p:BuildProjectReferences=false -p:OutDir=C:\Users\LUMAPA~1\AppData\Local\Temp\opencode\WmsBuild\` — passed, 0 errors. Existing compiler/analyzer warnings were reported.
- `graphify update .` — completed successfully.
- Manual browser retest after the code change was not performed in this session.

## Trade-offs and Remaining Validation

- The change is limited to the STR UoM dropdown; other dropdowns using the shared component were not changed.
- The regression test validates the SuiteQL filter translation. A browser retest should confirm the full STR interaction (type a UoM term, press Enter, observe narrowed options, then select a result) against the running application and NetSuite data.
