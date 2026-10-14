# Print Button on Document Grid Rows (STR & Returns to Supplier)

**Date:** 2026-10-10
**Status:** Implemented — builds clean, not yet functionally verified against NetSuite
**Scope:** `Web.BlazorServer`, `Integration.NS`, `Application.DataTransferObjects`
**Related:** `changelogs.md` `[0.12.5]`; `.opencode/docs/NetSuite-Integration-Guide.md`

---

## Summary

Adds a per-row **Print** button to the Stock Transfer Request grid (all three tabs) and the
Returns to Supplier grid, alongside the existing row action. Existing print buttons on the
corresponding form pages were migrated onto two new shared abstractions.

---

## The bug found on the way

The feature looked like a pure UI change. It was not.

`StockTransferRequestDataGridVM.Id` exists, `int`-typed, and Mapster maps it faithfully from
`StockTransferRequestDataGridNSDTO` → `StockTransferRequestDataGridDTO` → the VM. **But none
of the three STR SuiteQL queries ever selected `t.id`.** Every row therefore deserialized with
`Id == 0`.

Wiring `NetSuitePrint.TransferOrder(context.Id)` against that property would have produced a
working-looking print button that opened `recordId=0` on **every single row**.

This is a pre-existing latent defect, not one introduced by this change. It stayed invisible
precisely because nothing read the property. It is worth scanning for other declared-but-unselected
DTO properties before trusting any similar field.

Supplier Return never had the property at any layer, so it was an honest gap rather than a trap.

---

## Why the printout needs a NetSuite internal id

The scriptlets are keyed by internal id, not document number. Evidence gathered from the codebase:

1. Every print URL binds its last parameter to an internal id — `&recordId={Model.Id}`
   (`TripTicketCVU.razor.cs`, `SupplierReturnForm.razor.cs`, `STRForm.razor.cs`) or `&id={Model.Id}`
   (`ReceivingPage.razor.cs`, `PackingPage.razor.cs`).
2. Transfer Order, Returns to Supplier and Trip Ticket all use the same script (`1671`), so they
   share one id format.
3. The NetSuite SQL layer aliases the column as an internal id throughout — e.g.
   `NS_VendorReturnAuthorization_Get_PendingReturn.sql`: `t.id AS NetsuiteOrderInternalId`.
4. Every grid that *already* had a working row-level print button selects `t.id` explicitly:
   `ReceivingIntegration.cs` (`ItemReceiptDataGridDTO.Id`), `InventoryAdjustmentIntegration.cs`,
   `InventoryTransferRequestIntegration.cs`.

So no new NetSuite capability was required — only the missing column.

---

## Design

Two new shared pieces replace what was previously copy-pasted across every component.

### `NetSuitePrint` (static)

`Web.BlazorServer/Components/Shared/Others/NetSuitePrint.cs`

Builds scriptlet URLs. Returns `null` for a `null`/non-positive id, which is the signal callers
use to omit the control entirely.

```csharp
public static string? TransferOrder(int? recordId)  // TO + Intercompany TO
public static string? VendorReturn(int? recordId)   // Returns to Supplier
public static string? TripTicket(int? recordId)     // Trip Ticket (for migration)
```

Transfer Order and Intercompany Transfer Order were previously two **byte-identical** constants
selected by a dead ternary on `Category.IsInterCompany`. They now collapse into one builder.

### `AppPrintLink` (component)

`Web.BlazorServer/Components/Shared/Abstraction/AppPrintLink.razor`

Wraps the `RadzenButton` + `RadzenLink target="_blank"` pair that was duplicated at 8 call sites.
Renders nothing when `Url` is null or the caller's `Visible` gate is false.

**Icon-only by design.** The original markup put `Icon="print"` on the button *and* an icon plus a
`Text="Print"` on the nested link, producing a doubled icon with a text label in a 55–70px column.
The control now renders a single icon and keeps the label in the `title` / `aria-label` tooltip, so
it stays compact in a grid row while remaining identifiable to assistive tech and on hover.
Because the control no longer renders text, the `Text` parameter was removed from the component —
callers pass only `Url`, `Size` and optionally `Visible`.

### Gating decision — why there is no `IsEditable` check

Grid rows carry no read-only flag, so a per-row condition is required. The obvious candidate was
`StockTransferRequestDataGridVM.IsEditable`, which is defined as `StatusId == 3`
(`StockTransferRequestDataGridNSDTO.cs`).

**That would have been a bug.** Both grids source from the NetSuite `transaction` table, so *a row
being present already means it exists in NetSuite and is printable*. Gating on `!IsEditable` would
have hidden print on precisely the status-3 rows that were most printable, while showing it
elsewhere — a feature that appears and disappears for reasons unrelated to printing.

The form-level `Visible="@ReadOnly"` gate is retained, because a form *can* hold an unsaved,
not-yet-submitted document with no NetSuite id. `NetSuitePrint` returning null covers that case
defensively.

---

## Changes

| File | Change |
|---|---|
| `Integration.NS/.../StockTransferRequestIntegration.cs` | Added `("t.id", …)` to all three selects: `BuildIntercompanyBaseQuery`, `GetReturnsList`, `BuildTransferOrderBaseQuery` |
| `Integration.NS/.../SupplierReturnIntegration.cs` | Added `("t.id", nameof(SupplierReturnDataGridDTO.Id))` to `GetReturnsDataGridAsync` |
| `Application.DataTransferObjects/.../SupplierReturnDataGridDTO.cs` | Added `Id` |
| `Web.BlazorServer/ViewModels/.../SupplierReturnDataGridVM.cs` | Added `Id`, marked `[QuickDataGridIgnore]` |
| `Web.BlazorServer/.../STRDataGrid.razor` / `.razor.cs` | Print button in the frozen action column (widened 35px → 55px); `PrintUrl` helper |
| `Web.BlazorServer/.../SupplierReturnsDataGrid.razor` / `.razor.cs` | Print button in `<RowActions>`; `ActionWidth="55"`; `PrintUrl` helper |
| `Web.BlazorServer/.../STRForm.razor` / `.razor.cs` | Removed duplicate constants + dead ternary; now uses `NetSuitePrint` + `AppPrintLink` |
| `Web.BlazorServer/.../SupplierReturnForm.razor` / `.razor.cs` | Same migration |
| `Web.BlazorServer/.../NetSuitePrint.cs` | **New** |
| `Web.BlazorServer/.../AppPrintLink.razor` | **New** |

`[QuickDataGridIgnore]` on the new `SupplierReturnDataGridVM.Id` is required: `QuickDataGrid`
reflects over public properties to auto-generate columns, and would otherwise render a stray
"Id" column. `DateLastModified` already uses this attribute for the same reason.

---

## Deliberately out of scope

Six call sites still carry their own hardcoded URL constants and were **left untouched** so this
change stays a single reviewable unit:

- `PackingPage.razor.cs` (script 1922)
- `ReceivingPage.razor.cs` (script 1914)
- `InventoryCountingCVUPage.razor.cs` and `InventoryCountingNetSuiteViewPage.razor.cs` (script 1927)
- `TripTicketCVU.razor.cs` (script 1671 — can migrate straight onto `NetSuitePrint.TripTicket`)
- Packing detail components: `STRPackingDetails`, `ReturnsPackingDetails`,
  `VendorReturnAuthorizationPackingDetails` (script 1922)

Note the packing/receiving/counting scriptlets are inconsistent about their trailing parameter
(`&id=` vs `?id=`). Each should be verified against its own call site during migration rather than
batch-assumed.

**This is now the highest-value follow-up:** the NetSuite `ns-at` session token is a credential
that expires. When it rotates, these six sites must each be found and fixed by hand.

---

## Verification

```
dotnet build Web.BlazorServer/Web.BlazorServer.csproj   ->  Build succeeded, 0 Error(s)
```

> A full-solution build currently fails with 16 `MSB3021`/`MSB3027` file-copy errors. These are
> **file locks**, not compile errors: a running `Web.BlazorServer` process (PID 29928) and Visual
> Studio hold the output DLLs. Building to a redirected output path succeeds.

**Not yet verified:**

- The print buttons have not been exercised against a live NetSuite account. The `t.id` selection
  change needs a smoke test on all three STR tabs plus Returns to Supplier.
- STR grid queries carry documented performance sensitivity — a ~45s NetSuite full scan exists when
  the purchase-category predicate is pushed to the server
  (`StockTransferRequestIntegration.cs`). Adding a column to `SELECT` adds no join and changes no
  predicate, so the ~1–2s envelope should hold, but the Returns tab executes live paged
  SuiteQL rather than the `FetchAllBaseRowsAsync` fast path and is worth timing against prod.

---

## Operational notes

- **Both grids are cached for 60s** (`CachedStockTransferRequestIntegration`,
  `CachedSupplierReturnIntegration`). The cache is in-memory, so it clears on restart, but rows
  cached before a deploy will not carry `t.id`.
- **Clear the grid cache on release** so the print buttons are immediately usable.
- `NetSuitePrint` currently hardcodes the `ns-at` session token, exactly as the previous call sites
  did. Moving it to `IConfiguration` (or per-user options) is the durable fix and is flagged as a
  TODO in the file. It is a small, separable change.