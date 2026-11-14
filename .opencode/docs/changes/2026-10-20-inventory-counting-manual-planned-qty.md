# Inventory Counting — Manual Entry must not cap the count by Planned Qty

Date: 2026-10-20
Area: Mobile.MAUI `ManualEntryDialog` (shared reuseable), `InventoryCountingItemView`
Related: `2026-10-20-inventory-counting-mobile-fixes.md` (introduced Manual Entry for counting)

## Problem

On the mobile Inventory Counting item screen, Manual Entry refused to accept a count whenever the
line's planned quantity was 0 (or lower than the counted quantity). Reported example:

| Field | Value |
|---|---|
| Item | `TEST ITEM 1000` |
| Planned Qty | `0` |
| Remaining Qty | `-30` |
| Good Qty | `30` |
| Bad Qty | `0` |
| Error | `Total Qty cannot exceed Planned Quantity).` |

Confirm was blocked, so the item could not be counted at all.

## Root cause

`ManualEntryDialog` is a **shared** component used by seven screens. It embedded the
receiving/packing rule "you cannot receive more than is expected" directly in `OnConfirm`:

```csharp
// before — ManualEntryDialog.razor.cs
if (GoodQty + BadQty + MissingQty > PlannedQty)
{
    ValidationMessage = $"Total Qty cannot exceed Planned Quantity).";
    return;
}
```

`InventoryCountingItemView.OpenManualEntry` passed the counting line's planned qty as `PlannedQty`
(`item.LineQuantity`). For a count, the planned/system quantity is an **expectation being verified**,
not a ceiling: it can be `0` for an unplanned item, and the physical count legitimately exceeds it
(unexpected find). `30 > 0` therefore failed the check, and `RemainingQty = PlannedQty - (Good + Bad)`
rendered as `-30`.

The previous inventory-counting change set `AllowZero` and `ShowMissing`, but left the planned-qty cap
in place, so the counting flow inherited a receiving-only constraint.

## What changed

### `Mobile.MAUI/Components/Reusables/ManualEntryDialog.razor.cs`

- New parameter, defaulted so **all existing callers keep today's behaviour**:

  ```csharp
  /// <summary>
  /// When true, the total entered may not exceed PlannedQty and Planned/Remaining Qty are shown.
  /// Counting screens set this to false: a count is not bounded by the system quantity.
  /// </summary>
  [Parameter] public bool EnforcePlannedLimit { get; set; } = true;
  ```

- The cap is now conditional on that flag:

  ```csharp
  private decimal TotalQty => GoodQty + BadQty + MissingQty;

  if (EnforcePlannedLimit && TotalQty > PlannedQty)
  {
      ValidationMessage = "Total Qty cannot exceed Planned Quantity.";
      return;
  }
  ```

- The duplicate sum in the `AllowZero` check now reuses `TotalQty`, so the two rules cannot drift.
- Fixed the stray closing parenthesis in the message (`Quantity).` → `Quantity.`).

### `Mobile.MAUI/Components/Reusables/ManualEntryDialog.razor`

- The `Planned Qty` and `Remaining Qty` blocks are wrapped in `@if (EnforcePlannedLimit)`.
  When the limit is not enforced they are hidden: `Remaining Qty` would otherwise display a
  meaningless negative value and bias the counter toward the system quantity (blind count).

### `Mobile.MAUI/Components/Pages/InventoryCounting/InventoryCountingItemView.razor.cs`

- `OpenManualEntry` now opts out of the limit:

  ```csharp
  { "AllowZero", true },
  { "EnforcePlannedLimit", false }
  ```

  `PlannedQty` is still passed (harmless; it is simply not displayed or enforced).

## Why a flag instead of removing the rule

The other six callers rely on the cap because it prevents over-receiving/over-packing against a
document line:

| Screen | Call site |
|---|---|
| Receiving — Purchase Order | `PurchaseOrderItemView.razor.cs:324` |
| Receiving — Transfer Order | `TOxItemFulfillmentItemView.razor.cs:321` |
| Receiving — Returns | `TOxReturnxItemFulfillmentItemView.razor.cs:262` |
| Packing — Transfer Order | `TransferOrderItemView.razor.cs:440` |
| Packing — Returns | `ReturnsItemView.razor.cs:235` |
| Packing — Vendor Return Authorization | `VendorReturnAuthorizationItemView.razor.cs:285` |

Because `EnforcePlannedLimit` defaults to `true`, those call sites are untouched. Only the counting
screen declares its different rule.

## Not changed (verified, no action needed)

- **Zero counts still persist.** The mobile save sends `IsCounted = true` and
  `NetSuiteApiClientService.cs:2004` filters on `IsCounted || ScannedQuantity > 0`, so a line counted
  as 0 is sent and NetSuite records the zero. The `ScannedQuantity > 0` filter in
  `Web.BlazorServer` `PatchStartedInventoryCountingAsync` is the web path only, which never sets
  `IsCounted`; it is unaffected.
- **Line state after manual entry** (`goodLine`/`badLine` `ScannedQuantity`, `IsCounted`) is unchanged.
- `ScanCount`, `ShowMissing` and `Min="0"` behaviour are unchanged.

## Verification

- `dotnet build Mobile.MAUI` — **0 errors** (pre-existing warnings only, no new ones from these files).
- Manual checks still required on a device/sandbox:
  1. Count a line whose Planned Qty is `0` — Manual Entry shows only Item / Good Qty / Bad Qty, and
     Confirm accepts `30`.
  2. Count a line above its planned quantity (e.g. Planned `5`, Good `8`) — Confirm accepts it.
  3. Save, reopen the count — the over-planned quantity is retained.
  4. Regression: Manual Entry on PO receiving still blocks a quantity above `NSLineQuantityReceived`
     and still shows Planned / Remaining.
  5. Regression: Manual Entry on TO/Returns/VRA packing still blocks above the planned quantity.

## Rollback

Revert the three files. If only the caller is reverted (`EnforcePlannedLimit` absent), the dialog
defaults to enforcing the cap and counting returns to the blocked state. If only the dialog is
reverted, the counting screen would enforce the cap again; the other screens are unaffected either way.

## Follow-ups (not in this change)

- `ShowMissing` and `MissingQty` remain in the dialog but no Missing input is rendered; `MissingQty`
  is always 0. Either implement the field or remove it from the validation sum.
- `ValidationMessage` is not cleared when the operator edits a quantity after a failed Confirm.
- The quantity inputs are not labelled with a UoM; manual entry assumes the line UoM while scans
  convert through `UoMRate`.
