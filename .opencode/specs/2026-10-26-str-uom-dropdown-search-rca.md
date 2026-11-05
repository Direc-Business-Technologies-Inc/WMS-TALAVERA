# RCA: STR UoM Dropdown Search Does Not Filter

- **Date:** 2026-10-26
- **Status:** Confirmed
- **Mode:** RCA
- **Scope:** UoM search in the Stock Transfer Request creation flow when creating a transfer order.
- **Related:** None found

## Summary

When creating a transfer order through the STR flow, typing a UoM search term and pressing Enter triggers a dropdown data load, but the available UoMs remain unfiltered.

The dropdown stays open in the supplied screenshot. The issue is not that Enter closes the dropdown; the search filter is missing from the query intent.

## Impact

Users cannot narrow UoM choices using the dropdown search. Selecting a specific unit may be slower or more error-prone when an item has multiple units.

## Confirmed Root Cause

The STR UoM dropdown at `Web.BlazorServer/Components/Pages/Transaction/StockTransferRequest/Components/STRForm.razor:238` does not set the `FilterTarget` parameter.

In `Web.BlazorServer/Components/Custom/QuickVirtualizedDropdown.razor.cs:83–92`, the shared component adds a filter to the `DataGridIntent` only when both `args.Filter` and `FilterTarget` are non-empty. Because STR does not provide a target, the user's search text is not added to the intent.

The downstream path supports filters: `STRForm.razor.cs:286–290` passes the intent to `ItemsHandler.GetItemUnits`, and `Integration.NS/Implementations/Others/ItemsIntegration.cs:150–165` applies it through `.WithDatagridIntent(intent)` when building the SuiteQL query. The failure is therefore at the STR dropdown configuration, not in Enter handling or the provider/query path.

## Affected Paths and Potential Blast Radius

- **Confirmed affected:** STR line UoM dropdown.
- **Potentially affected:** Other `QuickVirtualizedDropdown` usages without `FilterTarget`. Whether they are affected depends on whether their search is expected to filter.
- **Not established:** Whether other dropdowns have the same user-visible symptom; they require separate verification.

## Recommended Corrective Direction

1. Configure the STR UoM dropdown with the correct filter target for the UoM name—expected to be `Name`, subject to checking the existing query-builder property mapping.
2. Verify that entering a search term adds a name filter to the intent and that the returned UoM list is narrowed.
3. Add regression coverage for filtered UoM results, and verify that unfiltered loading and selecting a UoM still work.

## Deferred Concerns

- Changing shared Enter-key behavior across Radzen dropdowns.
- Changing filtering behavior in other forms without confirming their requirements.
- Refactoring the shared dropdown or the item-unit provider path.
