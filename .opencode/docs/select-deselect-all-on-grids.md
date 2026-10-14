# Select/Deselect All on Checkbox Grids

**Date:** 2026-10-10
**Status:** Implemented — builds clean (Family A + B), **Family C not yet implemented**; not yet functionally verified
**Scope:** `Web.BlazorServer` only
**Related:** `STR-grid-perf-fix-summary.md` (page-scope decision); `.opencode/docs/print-button-on-document-rows.md` (same grid infrastructure)

---

## Summary

Adds a **Select/Deselect All** affordance to every checkbox grid in the app, surfaced in two
coordinated places:

1. a **tri-state header checkbox** in the frozen checkbox column's `HeaderTemplate`
   (checked / unchecked / indeterminate),
2. a **toolbar button** ("Select All" / "Deselect All", label flips with state).

Both live in one new shared component, `SelectAllHeader`, and are wired per grid into one of
three behavioural families:

| Family | Meaning | Grids | Status |
|---|---|---|---|
| **A** | Row selection feeds a document's line collection | `GoodsIssueItemSelection`, `GoodsReceiptItemSelection`, `InventoryCountingItemSelection`, `InventoryTransfer/ItemSelection`, `SalesReturnItemSelection`, `TripTicketFulfillmentSelection`, `ItemDataGrid` (7) | ✅ Done |
| **B** | Row flag `IsReceived` on receipt forms | `ItemReceiptForm`, `PackingItemReceiptForm`, `ReturnsItemReceiptForm`, `VendorReturnAuthorizationItemReceiptForm` (4) | ✅ Done |
| **C** | Per-permission toggles on Authorization grids | `RoleAuthorization`, `UserAuthorization` | ⛔ Not started |

11 grids implemented across 25 modified files + 2 new files.

---

## The bug found on the way

The feature surfaced a **pre-existing latent defect** in `AppDataGrid`'s selection pruning.

After every data load, `AppDataGrid` pruned `SelectedItems` down to items present in the new page
using `Contains(d)` — a **reference-identity** check. That worked while rows were re-selected by
clicking the *same* in-memory objects, but the `DataGetter` returns **freshly deserialized VMs on
every query**. The moment selection + paging intersect — exactly what Select-All encourages — the
old instances are no longer reference-equal to the new page's instances, the `Contains` check
fails, and selected rows are **silently dropped**. Select-All would have looked broken on the
second page turn for reasons having nothing to do with the new code.

Fixed with key-based reconciliation:

- `SelectionKey()` — static helper reflecting over `ItemCode` → `ItemNumber` → `OrderNumber` →
  `Id`, first existing property wins.
- Pruning now matches **by key** and re-points `SelectedItems[i]` to the fresh instance **in
  place**, preserving order and identity of the caller's list.
- Skipped entirely when keys are non-unique (safe fallback: keep existing behaviour rather than
  collapse distinct rows).
- The fresh page list is materialised with `.ToList()` because `DataGrid.Data` is typed
  `IEnumerable`.
- `using System.Reflection;` added for the property lookup.

---

## Design

### `SelectAllHeader` (shared component)

`Web.BlazorServer/Components/Shared/Abstraction/SelectAllHeader.razor` / `.razor.cs`

Inherits `BaseComponent` (explicit `using Web.BlazorServer.Components.Base;` per project
convention). Renders a `RadzenCheckBox TriState` and/or a `RadzenButton`; the two halves are
independently switchable so each call site composes exactly one control per surface.

```csharp
[Parameter] public bool?          Value        { get; set; }   // true / false / null = indeterminate
[Parameter] public EventCallback<bool?> OnChange { get; set; }  // always receives true or false, never null
[Parameter] public bool           Disabled     { get; set; }
[Parameter] public bool           ShowCheckbox { get; set; } = true;   // toolbar usage sets false
[Parameter] public bool           ShowButton   { get; set; } = true;   // header-cell usage sets false
[Parameter] public string         SelectText   { get; set; } = "Select All";
[Parameter] public string         DeselectText { get; set; } = "Deselect All";
[Parameter] public string         CheckboxName { get; set; } = $"select_all_{Guid.NewGuid():N}";

public static bool? Compute(int eligibleCount, int selectedCount)
```

Two behaviours worth knowing:

- **Tri-state normalisation.** Radzen's tri-state box cycles through `null` on click. The handler
  maps `null` → the *opposite of the current value*, so consumers only ever see an explicit
  `true` (select all) or `false` (deselect all). A click can therefore never leave the grid in a
  "do nothing" indeterminate state.
- **`Compute` is the single source of truth for header state:**

  ```csharp
  eligibleCount <= 0        -> false   // nothing selectable; show unchecked, not null
  selectedCount  <= 0       -> false
  selectedCount >= eligible -> true    // fully selected
  otherwise                 -> null    // indeterminate
  ```

  Callers pass **eligible** counts (Family B excludes `IsComplete` rows), so a disabled or
  completed row can never drag the header into a false indeterminate state.

### Family A — selection grids (✅)

Contract enforced across all 7:

- **"All" means the currently loaded page only** (`DGResult.Items`). Paging the whole data set is
  never triggered from the toggle — grid queries routinely cost 1–2 s
  (`STR-grid-perf-fix-summary.md`), so Select-All must not smuggle in a full fetch.
- Row handling was split into **idempotent cores**: `ApplyRowSelect` / `ApplyRowDeselect`
  (or `ApplyItemSelection` where the original handler toggled). The original `OnRowSelect` /
  `OnRowDeselect` wrappers keep calling them, then fire `*Changed.InvokeAsync` +
  `StateHasChanged` exactly as before — so single-row behaviour is bit-for-bit unchanged.
- `ToggleAllItems(bool?)` loops `PageItems`, calls only the **core** (no per-row event fan-out),
  then fires `UnsavedChangesService.MarkDirty()` **once** and the change callback **once** at the
  end. This keeps `Document.DocumentLines` / `Issue.DocumentLines` / `Receipt.DocumentLines`
  consistent with the checkbox state without N change events per toggle.
- Where the original row handler **toggles** rather than sets (A1 grids: `ItemDataGrid`,
  TripTicket), the bulk loop guards with `if (select != isSelected)` so each row is acted on at
  most once.


Family A splits further by grid type:

- **A2 (row-click driven)** — the five `*ItemSelection` grids. Row checkboxes deliberately have
  **no `Change` handler**: selection already flows from the Radzen row click. Adding a `Change`
  handler would double-toggle every row on click. Header-only wiring is correct here.
- **A1 (explicit checkbox driven)** — `ItemDataGrid` and `TripTicketFulfillmentSelection`. Their
  icon-button pair (`check_box` / `check_box_outline_blank`) was replaced with a real
  `RadzenCheckBox` bound to selection state (`Value` reads selection, `Change` calls the row
  handler). `ItemDataGrid` additionally:
  - disables header + toolbar when `SelectionMode != SelectionModes.Multiple`
    (Select-All on a single-select grid is meaningless),
  - calls `SelectItem` per differing row inside `ToggleAllItems` — safe because in Multiple mode
    `SelectItem` is a pure set/remove, and it emits no per-row notifications in that branch.

### Family B — receipt `IsReceived` tables (✅)

Four receipt forms, each wired in two places:

- header checkbox inside the `IsReceived` checkbox column's `HeaderTemplate`
  (`ShowButton=false` — the column is too narrow for a text button),
- toolbar instance (`ShowCheckbox=false`) above the lines table.

`GetIsReceivedHeaderState()` / `ToggleAllIsReceived(bool?)` **skip rows with `IsComplete = true`**
— completed receipt lines are immutable, so bulk operations must never touch them. `Compute`
therefore receives *eligible* (non-complete) counts, not raw row counts.

After toggling, the forms notify the edit form explicitly:

```csharp
EditContext.NotifyFieldChanged(new FieldIdentifier(Data, nameof(Data.Lines)));
```

> ⚠️ **Gotcha:** there is no `EditContext.OnFieldChanged(...)` API in this project's Blazor
> version — an earlier attempt to call it does not compile. `NotifyFieldChanged` with a
> constructed `FieldIdentifier` is the correct form; it is what makes the form register dirty
> state and enables Save/Submit.

### Family C — Authorization permission grids (⛔ not implemented)

`RoleAuthorization` / `UserAuthorization` still need:

- a tri-state header **per permission column** (6 permission columns each), gated on
  `Editable` / `HasPermission` so unauthorized toggles stay inert;
- a global toggle looping rows and calling `OnPermissionChange()` per row;
- care to respect `MergeChanges()` idempotency (do not enqueue the same change twice).

---

## Files changed

| File | Change |
|---|---|
| `Web.BlazorServer/.../Shared/Abstraction/SelectAllHeader.razor(.cs)` | **New** — tri-state checkbox + optional Select/Deselect button + `Compute` |
| `Web.BlazorServer/Components/Shared/Abstraction/AppDataGrid.razor.cs` | Pruning fix: reference `Contains(d)` → key-based reconciliation via `SelectionKey()`; re-points `SelectedItems` in place; `.ToList()`; `using System.Reflection` |
| `Web.BlazorServer/Components/Custom/ItemDataGrid.razor(.cs)` | Icon-button pair → `RadzenCheckBox`; toolbar + header `SelectAllHeader`; `PageItems` / `GetSelectionHeaderState` / `ToggleAllItems` with Multiple-mode gate |
| `Web.BlazorServer/.../GoodsIssue/Components/GoodsIssueItemSelection.razor(.cs)` | Toolbar + header wiring; `ApplyRow*` split; page-scope toggle |
| `Web.BlazorServer/.../GoodsReceipt/Components/GoodsReceiptItemSelection.razor(.cs)` | Same |
| `Web.BlazorServer/.../InventoryCounting/Components/InventoryCountingItemSelection.razor(.cs)` | Same |
| `Web.BlazorServer/.../InventoryTransfer/Components/ItemSelection.razor(.cs)` | Same |
| `Web.BlazorServer/.../SalesReturn/Components/SalesReturnItemSelection.razor(.cs)` | Same |
| `Web.BlazorServer/.../TripTicket/Component/TripTicketFulfillmentSelection.razor(.cs)` | Icon button → `RadzenCheckBox`; `ApplyItemSelection` split (original body was a toggle); guarded bulk loop; `MarkDirty` once |
| `Web.BlazorServer/.../Receiving/Components/ItemReceiptForm.razor(.cs)` | Family B: header `HeaderTemplate` + toolbar; `IsComplete` skip; `NotifyFieldChanged` |
| `Web.BlazorServer/.../Packing/STR/ItemReceipt/Components/PackingItemReceiptForm.razor(.cs)` | Same |
| `Web.BlazorServer/.../Packing/Returns/ItemReceipt/Components/ReturnsItemReceiptForm.razor(.cs)` | Same |
| `Web.BlazorServer/.../Packing/VendorReturnAuthorization/ItemReceipt/Components/VendorReturnAuthorizationItemReceiptForm.razor(.cs)` | Same |

`ItemSelectionDialog.razor` and the Inventory Adjustment picker consume `ItemDataGrid` and
therefore inherit the feature without direct edits.

---

## Deliberately out of scope

- **Select-All across all pages.** Tempting, but every `DataGetter` here is a 1–2 s query
  (worse against live NetSuite SuiteQL). The header means "all rows currently loaded".
- **`Change` handlers on A2 row checkboxes.** Row selection already flows through the Radzen row
  click; a second handler would double-toggle. Header-only is intentional.
- **Family C (Authorization).** Untouched in this change — see above. This is the remaining
  follow-up before the feature can be called complete app-wide.
- Row-level visual changes beyond the A1 icon→checkbox swap: no columns were added, removed or
  reordered, and frozen-column widths are unchanged.

---

## Verification

```
dotnet build Web.BlazorServer/Web.BlazorServer.csproj   ->  0 Error(s)
```

Build was verified three times: after Family B; mid Family A (temporarily broken — handlers were
still missing in `InventoryCounting` / `InventoryTransfer`, and one refactor left an `await`
inside a newly-sync `ApplyItemSelection`); and after final repair.

**Not yet verified (manual, needs a running app on `http://localhost:8079`):**

- [ ] Family B: `IsComplete` rows untouched by bulk toggle; `NotifyFieldChanged` makes the form
      dirty enough to Save without an extra manual field edit.
- [ ] Family A: header states (unchecked / indeterminate / checked) match page contents;
      document lines on the parent form match the checkbox state exactly (no dupes/misses).
- [ ] Family A paging: select page 1 → visit page 2 → return — selection survives (the
      `SelectionKey` pruning fix; previously items vanished).
- [ ] A2 single-row clicks still toggle exactly once (no double-toggle regression).
- [ ] `ItemDataGrid` header/toolbar disabled in Single mode.
- [ ] Trip Ticket: select-all → deselect-all restores `Destinations` / `ToSubsidiaries` cleanly.
- [ ] Dirty-flag (`UnsavedChangesService.MarkDirty`) fires exactly once per bulk toggle.

> Menu routes are DB-driven and permission-scoped; if a menu entry is missing, navigate by URL
> (e.g. `/transactions/inventory/goods-issue/create`). Prerequisites for opening each picker:
> a selected **Warehouse**, and being in **create** (not view) mode — the green `+` stays hidden
> or disabled otherwise. Receipt forms are reached via Receiving/Packing list pages → details
> panel → **Create Item Receipt** / **Fulfill Item**.

---

## Operational notes

- Radzen.Blazor **9.0.8**. The tri-state `RadzenCheckBox` emits `null` as part of its click
  cycle; `SelectAllHeader` absorbs that. Consumers must not assume raw Radzen semantics leak
  through — `OnChange` is documented as `true`/`false` only.
- `AppDataGrid`'s key-based pruning uses reflection (`SelectionKey`) on hot paths. The lookup is
  first-match over four candidate property names and is cheap relative to a grid reload, but if
  selection lists grow to thousands of rows, caching the resolved `PropertyInfo` per `TItem` is
  the natural optimisation.
- The pruning fix needs **no cache invalidation** on deploy — it operates on in-memory component
  state, which resets per circuit/restart anyway.
- If a future grid's row type lacks all four key properties (`ItemCode`, `ItemNumber`,
  `OrderNumber`, `Id`), pruning silently falls back to keep-all rather than dropping rows.
  Adding a distinct key property to the row VM is the fix.
- **Follow-up:** implement Family C, then tick the verification list and update the Status line
  at the top of this document.

