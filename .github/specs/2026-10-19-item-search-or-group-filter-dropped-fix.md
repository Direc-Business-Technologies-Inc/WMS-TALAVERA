# Fix: Item picker search does nothing (OR-group filter dropped as "unmapped property ''")

Date: 2026-10-19
Status: Fixed in source, `dotnet build Integration.NS` clean, 109/109 filter/query tests green — **client-confirmed 2026-10-19 (web): searching `ZZZZNOPE` returns "No records", real terms narrow the grid, and the `Dropped grid filter on unmapped property ''` warning is gone**.
Mode: Bugfix — query-builder filter parsing
Scope: `Integration.NS` SuiteQL filter builder; consumer is the web item picker used by STR / ITR / Supplier Return / Inventory Adjustment (`ItemSelectionDialog` → `ItemDataGrid`)
Related:
- `Web.BlazorServer/Components/Custom/ItemDataGrid.razor.cs` — the search box that builds the group (`:66-93`)
- `Shared.Libraries/Entities/AppFilterDescriptor.cs` — `Property` defaults to `string.Empty`; groups are `LogicalOperator` + `Filters`
- Commit `f537d36f` ("fix: Add Search Bar in Item Selection") — the search was introduced with this group shape and therefore never actually filtered

Changed files:
- `Integration.NS/Services/SuiteQLQueryBuilder.cs` — `_parseFilter` resolves a group before its own `Property`; `_parseFilterGroup` drops unresolved children and returns `string?`
- `Integration.NS.Tests/Services/SuiteQLUnresolvedPropertyGuardTests.cs` — 2 new regression cases

## Summary

Searching in the item selection picker returned **the same rows** as the unfiltered grid. The operator sees the search box accept input, but the grid never narrows.

Log evidence (the tell):

```
Integration.NS.Services.SuiteQLQueryBuilderFactoryService: Warning: Dropped grid filter on unmapped property '': it is neither in the property map, a SELECT alias, nor a qualified column, and would be rejected by NetSuite with 400 INVALID_PARAMETER.
```

The empty `''` is the search **OR-group**, not a column. The group was discarded whole, so its valid `ItemNumber` / `Name` / `Description` children were never parsed and the effective `WHERE` stayed identical to the unfiltered load (only the always-present `QuantityAvailable > 0` filter remained).

This was **not** a caching problem and **not** a search-bar event-wiring problem (both were investigated and cleared — see *Alternatives Rejected*).

## Root Cause

The search box builds a filter group with no property of its own:

`Web.BlazorServer/Components/Custom/ItemDataGrid.razor.cs:66-93`
```csharp
var searchFilter = new AppFilterDescriptor
{
    LogicalOperator = LogicalOperatorEnum.OR,   // <-- no Property; defaults to ""
    Filters =
    [
        new AppFilterDescriptor { Property = nameof(ItemsVM.ItemNumber), Value = SearchText, ComparisonOperator = ComparisonOperatorEnum.Contains, ... },
        new AppFilterDescriptor { Property = nameof(ItemsVM.Name), ... },
        new AppFilterDescriptor { Property = nameof(ItemsVM.Description), ... }
    ]
};
```

`SuiteQLQueryBuilder._parseFilter` resolved the descriptor's own `Property` **before** checking whether the descriptor was a group, and returned early when `ResolveGridProperty` yielded null:

`Integration.NS/Services/SuiteQLQueryBuilder.cs` (before)
```csharp
var prop = ResolveGridProperty(filter.Property, _gridSourcedDescriptors.Contains(filter));
if (prop is null)
{
    logger?.LogWarning("Dropped grid filter on unmapped property '{Property}': ...", filter.Property);
    return null;                       // <-- empty group Property lands here
}
if (filter.Filters.Count > 0) return _parseFilterGroup(filter);   // <-- never reached
```

A group is a **container**: it has no column, only children, so resolving its `Property` is meaningless. Because the group's property was `""`, the early return fired, the group was dropped, and the search predicate never reached the SQL.

## The Fix

### 1. Resolve groups before their own property

`SuiteQLQueryBuilder._parseFilter` — handle the group first, so an empty/absent group `Property` can no longer discard it:

```csharp
// A group is a container: it holds no column of its own, only children.
if (filter.Filters.Count > 0) return _parseFilterGroup(filter, propertyMap);

var prop = ResolveGridProperty(filter.Property, _gridSourcedDescriptors.Contains(filter));
if (prop is null) { /* existing warning */ return null; }
```

Leaves `Property`-bearing groups and leaf filters (radzen column filters, hand-written `WithFilters`) exactly as before.

### 2. Drop unresolved children before joining the group

`SuiteQLQueryBuilder._parseFilterGroup` — `string.Join` over a null child emitted an empty segment, i.e. malformed SQL such as `( OR LOWER(x) LIKE ...)`. Now unresolved children are filtered out, and an all-dropped group collapses to `null` like any other dropped filter (signature widened to `string?`):

```csharp
var parts = filter.Filters
    .Select(child => _parseFilter(child, propertyMap))
    .Where(part => part is not null)
    .ToList();

return parts.Count == 0 ? null : "(" + string.Join($" {op} ", parts) + ")";
```

### Where the fix belongs

The builder, not `ItemDataGrid`. `DataGridIntentAdapter.AdaptToMyFilterQueries` already produces group descriptors on every radzen column filter, so "a group is a valid descriptor" is established contract — it merely happened to rely on the group carrying a resolvable column. A one-line workaround in the grid (give the search group `Property = nameof(ItemsVM.ItemNumber)`) would work but is semantically false and silently re-breaks if that column stops being mapped; the builder fix repairs every group producer and is what was applied.

## Tests

**New** — `Integration.NS.Tests/Services/SuiteQLUnresolvedPropertyGuardTests.cs`:
- `Build_GridFilterGroupWithNoProperty_StillAppliesItsChildren` — a `DataGridFilterUtilities.Any(...)` OR-group (the exact shape the search box builds) now emits both `LOWER(...) LIKE ...` predicates and an `OR`.
- `Build_GridFilterGroup_DropsUnresolvedChildWithoutMalformedSql` — a group mixing a resolvable and an unresolvable child keeps the resolvable predicate and produces no `( OR` / `OR )` / `()`.

**Existing and still green:** the 5 prior `SuiteQLUnresolvedPropertyGuardTests` cases (leaf-filter drop/passthrough, PropertyMap precedence, alias expansion) and the broader filter/query-shape set — **109/109 passed**.

Build: `dotnet build Integration.NS` → **0 errors** (pre-existing warnings only).

## Verification Plan

**Result (client, web, 2026-10-19):** steps 1–3 pass — the search now filters, and the previously reported "same rows" symptom is gone.

1. Open the item picker (STR / ITR / Supplier Return / Inventory Adjustment), search `ZZZZNOPE` → grid shows **"No records"**, and the `Dropped grid filter on unmapped property ''` warning **no longer appears** in the log. **Verified.**
2. Search a real term → the row set narrows to matches on item id, name, or description. **Verified.**
3. Clear the search → the full `QuantityAvailable > 0` set returns. **Verified.**
4. Not a caching/repaint fix: identical inputs still legitimately cache for 60 s; the change is purely about the filter reaching the SQL.

## Decisions and Alternatives Rejected

| Decision | Alternatives rejected and why |
|---|---|
| **Fix in the builder (group-first)** (chosen) | *Set a `Property` on the grid's search group*: unblocks one caller, leaves the same trap for the next group author, and re-breaks if that column is unmapped. |
| **Drop unresolved children in `_parseFilterGroup`** (chosen) | *Leave `string.Join` as-is*: unreachable for this bug, but any future partially-resolved group would emit malformed SQL that NetSuite 400s. |
| **Cache ruled out** | `GridResultCache.BuildKey` serializes the full `DataGridIntent` including filter values, so a new search term is a distinct key → cache miss. Caching cannot return another filter's page. |
| **Event wiring ruled out** | `RadzenTextBox` (Radzen 9.0.8, web) splats `@attributes` onto the `<input>`; with the default `Immediate=false` there is no internal `oninput`, so the handler fires and `SearchText` is set before the query is built. |

## Residual Risk

1. **Resolved — client-confirmed 2026-10-19 (web).** The operator reproduced the previously broken search and confirmed it now filters (`ZZZZNOPE` → "No records", a real term narrows the grid); the `Dropped grid filter on unmapped property ''` warning no longer logs.
2. **Low — the search group still carries no `Property`.** Correct for a container; if a future refactor reintroduces a property-resolution requirement ahead of the group check, regress. The new tests guard it.
3. **Low — `ItemDataGrid` remains untested directly** (no `Web.BlazorServer` test project); coverage is at the builder boundary, which is where the defect was.
4. **Informational — `TaskCanceledException` ×3 in the debugger output** are debounce / superseded-load cancellations, expected and unrelated.

## Follow-up Actions

| # | Action | Status |
|---|---|---|
| 1 | `_parseFilter` group-first; `_parseFilterGroup` null-safe | **Done** |
| 2 | Regression tests (2 cases) | **Done** |
| 3 | `dotnet build Integration.NS`; filter/query test sweep | **Done — 0 errors, 109/109** |
| 4 | `graphify update .` | **Done — 18713 nodes, 44115 edges** |
| 5 | Client visual confirmation (`ZZZZNOPE` empty, warning gone) | **Done — client-confirmed 2026-10-19** |
