# Grid Paging State Stability

## Summary

Editable paged grids must receive a stable `Data` collection reference across normal Blazor rerenders. Creating a new collection in Razor markup or in a computed property can cause `RadzenDataGrid` to treat the data source as replaced and reset its current page to page 1.

This was reported on packing item-receipt pages when an operator changed the Good or Bad quantity, or toggled a line, while viewing page 2 or later.

## Root Cause

Blazor rerenders a component after bound controls change. The affected grids materialized a new list during each render:

```razor
Data="[..FormData.DocumentLines]"
```

The packing forms had the equivalent behavior in a computed property:

```csharp
List<LineVM> FulfillableLines => [.. Data.Lines.Where(line => !line.IsComplete)];
```

Although the row objects did not change, the list reference changed. Radzen then reset the grid's paging state.

## Resolution

- Pass existing `List<T>` instances directly to `AppTable` when no transformation is required.
- Cache filtered packing lists and rebuild them only when the underlying `Data.Lines` list is replaced.
- Do not copy `AppTableWithSettings.Data` during rendering.

The fix covers these areas:

- Stock transfer request packing item receipt
- Returns packing item receipt
- Vendor return authorization packing item receipt
- Goods issue
- Goods receipt
- Sales order
- Delivery
- Trip ticket fulfillment
- Inventory counting and inventory counting sheet creation
- All `AppTableWithSettings` consumers

## Implementation Rule

Prefer a stable reference:

```razor
<AppTable Data="@FormData.DocumentLines" />
```

Avoid render-time collection creation:

```razor
<AppTable Data="[..FormData.DocumentLines]" />
```

When filtering is required, store the filtered result in a field. Refresh that field only when its source is loaded or intentionally replaced. Do not use a computed property that calls `ToList()` or a collection expression on every render.

## Regression Verification

For every editable paged grid:

1. Load enough rows to show at least two pages.
2. Navigate to page 2.
3. Toggle a row checkbox or switch.
4. Edit numeric, text, and dropdown fields where available.
5. Commit an edit with Enter, Tab, and loss of focus.
6. Confirm the grid remains on page 2 after each interaction.
7. Confirm sorting, filtering, row selection, and edited values remain intact.
8. Add or remove rows where supported and confirm the current page changes only if the previous page is no longer valid.

## Review Checklist

When adding or reviewing an `AppTable` or `RadzenDataGrid`:

- Check that `Data` is not populated with `[..source]`, `source.ToList()`, or `source.Where(...)` directly from render markup.
- Check that computed `Data` properties do not allocate a new collection per getter call.
- Check that event handlers do not call `Reload`, `ReloadSettings`, or replace the backing list unless a data refresh is intended.
- Verify page retention after editing a row on page 2 or later.
