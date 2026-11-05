# STR Grid Performance Fix — Summary

**Date:** 2026-10-10
**Branch:** `fix/issue-logs-with-opti`
**Issue:** STR page (Transfer Orders / Intercompany / Returns tabs) took 40–50s to load; grid flickered `No records → Wait → rows → Wait → rows`.

## Root causes

1. **Slow NetSuite query (~45s/scan).** The TO/ICTO list queries sent `(purchase_category != 3 OR purchase_subcategory = 5)` to NetSuite. That OR-group across two custom fields resolves as a full-table scan. Proven by read-only SuiteQL probes:
   - Q0 `COUNT(*)` with the OR → **46s** (1 row, `total = 25`)
   - Q1 full query with the OR → **26s** (25 rows)
   - Q2 same query minus display lookups → **25s** (sort/DFs ≈ 1s — not the cause)
   - Q5 same query minus the OR → **1–2s** (base predicates are selective)
   - Q6 Q0 + 90-day date bound → **47–48s** (date filter doesn't help)
2. **Double-fetch per tab open.** `STRDataGrid` and child `AppDataGrid` both ran `LoadGridSettings → Reload()` on first render → 2 identical NetSuite calls; the 2nd early-returned empty via the `IsBusy` guard (the phantom `No records` + `Wait`). Confirmed in NSREQ logs as duplicate `limit=10&offset=0` pairs (#6/#8, #9/#11).
3. **Shared busy key.** All 3 tabs used one `ActionName` (`"get things from db"`), so mounted tabs collided on spinner state.

## Fix

- **Query:** removed the scan-driving OR-group from `GetTransferOrderList` / `GetIntercompanyTransferOrderList` (also fixed the trailing-space `"purchase_subcategory "` filter key and dropped the unused `ToSubsidiary` DF on the TO tab). The base query (~1–2s) is fetched whole (`FetchAllBaseRowsAsync`, 1000-row pages, 5000 cap), the equivalent predicate runs in memory (`PassesPurchaseCategoryFilter`, SQL-3VL-exact incl. NULL handling), then the filtered set is sorted + paged (`SliceFilteredPage`) so counts and pages stay correct.
- **Grid:** single reload owner (child `AppDataGrid` only) + per-tab `ActionName` (`str_to_list` / `str_icto_list` / `str_returns_list`).
- **DTO:** raw `PurchaseCategoryId` / `PurchaseSubcategoryId` (`int?`) selects added for the in-memory predicate.
- **Tests:** 6 new tests (OR absent from WHERE, raw-ID selects, in-memory filter incl. NULL 3VL cases, filtered-set paging); `CapturingNetSuiteClient` gained multi-row `NextItems`.

## Files changed

- `Integration.NS/Implementations/Transactions/StockTransferRequestIntegration.cs`
- `Integration.NS/DataTransferObjects/StockTransferRequest/StockTransferRequestDataGridNSDTO.cs`
- `Web.BlazorServer/Components/Pages/Transaction/StockTransferRequest/Components/STRDataGrid.razor.cs`
- `Web.BlazorServer/Components/Pages/Transaction/StockTransferRequest/Components/STRDataGrid.razor`
- `Web.BlazorServer/Components/Pages/Transaction/StockTransferRequest/StockTransferRequestPage.razor`
- `Integration.NS.Tests/Implementations/Transactions/StockTransferRequestIntegrationTests.cs`
- `Integration.NS.Tests/Fakes/CapturingNetSuiteClient.cs`

## Validation

- `Integration.NS` build: 0 errors; `Web.BlazorServer` build: 0 errors
- `Integration.NS.Tests`: **163/163 pass** (19 STR incl. 6 new)
- Manual: TO tab **40–50s → 1–2s**; debug `NSREQ` logging reverted to HEAD (not shipped)
- Data integrity: read-only change; same row set incl. NULL semantics (verified by tests); Returns tab, mutations, and payloads untouched

## Known edge

If a subsidiary's base set ever exceeds the 5,000-row fetch cap, the grid slices from the fetched window (rows beyond the cap hidden, no error). Current prod base set is 25 rows. Optional follow-up: surface a "refine filters" notice when the cap is hit.
