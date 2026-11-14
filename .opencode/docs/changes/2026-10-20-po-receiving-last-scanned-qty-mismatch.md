# PO Receiving — Last Scanned panel showed the wrong quantity

Date: 2026-10-20
Area: Mobile.MAUI `Receiving/PurchaseOrder/PurchaseOrderItemView`
Branch: `fix/mob-backlogs`
Status: implemented (manual device check pending)

## Symptom

On the PO Receiving item screen the same line showed two different "remaining" values:

- the list row (Good/Bad tabs) showed **9**
- the pinned "Last Scanned" panel showed **20**

Both were the grey comparison box under the yellow scanned-quantity box. The yellow
value was correct and identical in both places.

## Root cause

The panel was copied from the Packing views and kept their bindings:

| Binding | List row | Last Scanned panel (before) |
|---|---|---|
| comparison qty | `NSLineQuantityReceived` | `NSLineQuantityPacked` |
| complete flag | `AlreadyReceived` | `AlreadyPacked` |

`Shared.Libraries/ViewModel/Common/TransactionVM.cs:47-48`:

```csharp
public decimal NSLineQuantityReceived => (LineQuantity - (LineQuantityReceived + LineQuantityBackOrdered)) / UoMRate;
public decimal NSLineQuantityPacked   => (LineQuantity - (LineQuantityPacked   + LineQuantityBackOrdered)) / UoMRate;
```

On a Purchase Order `quantitypacked` is always 0 (packing belongs to item fulfillments), so
`NSLineQuantityPacked` collapses to the **whole ordered quantity** — 20 — while
`NSLineQuantityReceived` is the **open quantity still to receive** — 9. The missing 11 was
received earlier. `AlreadyPacked` had the same defect: the panel could never turn green on
a fully received line unless the operator scanned the full ordered qty in one session.

## Fix

`PurchaseOrderItemView.razor:35,38,42` — three tokens, no markup or logic change:

```diff
- class="... (LastScanned.AlreadyPacked ? "rz-background-color-success" : "rz-background-color-warning") ..."
+ class="... (LastScanned.AlreadyReceived ? "rz-background-color-success" : "rz-background-color-warning") ..."
- class="... (LastScanned.AlreadyPacked ? "rz-color-white" : "") ..."
+ class="... (LastScanned.AlreadyReceived ? "rz-color-white" : "") ..."
- <RadzenText Text="@FormatQuantity(LastScanned.NSLineQuantityPacked)" ...
+ <RadzenText Text="@FormatQuantity(LastScanned.NSLineQuantityReceived)" ...
```

The panel and the row now read the same property from the same `LastScanned` instance, so
they cannot disagree.

## Verification

- `dotnet build Mobile.MAUI -f net9.0-android` — **0 errors** (warnings pre-existing).
- Manual check against the PO in the client screenshot: expected `LineQuantity = 20`,
  `LineQuantityReceived = 11`, `QuantityPacked = 0`. Panel should now read **9**.
- On device: panel matches the row for both a partially received and a fully received line;
  a fully received line must show a **green** panel box.

## Duplication — NOT addressed

The 40-line panel markup exists in six views and has now diverged once. Only this view was
corrected; the rest already matched their own rows:

| View | Comparison binding | Correct for its screen? |
|---|---|---|
| `Receiving/PurchaseOrder` :42 | `NSLineQuantityReceived` | ✅ (fixed here) |
| `Receiving/TransferOrder/TOxItemFulfillment` :35 | `NSLineQuantityReceived` | ✅ |
| `Receiving/Returns/TOxReturnxItemFulfillment` :35 | `NSLineQuantityReceived` | ✅ |
| `Packing/TransferOrder` :35 | `NSLineQuantityPacked` | ✅ |
| `Packing/Returns` :34 | `NSLineQuantityPacked` | ✅ |
| `Packing/VendorReturnAuthorization` :35 | `NSLineQuantityPacked` | ✅ |

Two more views (`Receiving/TransferOrder/TransferOrderItemView`, `Receiving/Returns/ReturnsItemView`)
carry a single-value variant of the same panel.

Recommended follow-up (separate commit, not bundled with this hotfix): extract the panel into
one shared component parameterised by the comparison value and the complete flag, so the
Receiving/Packing distinction is expressed once instead of six times:

```razor
<LastScannedPanel Item="LastScanned"
                  CompareQty="LastScanned.NSLineQuantityReceived"
                  IsComplete="LastScanned.AlreadyReceived" />
```

Also still present: the empty `#recent-scanned` / `box-toolbar-page` placeholder
(`PurchaseOrderItemView.razor:18-20`). It is **load-bearing** as of
`.opencode/docs/changes/2026-10-20-last-scanned-panel-scroll-behaviour.md` — the panel's
scroll behaviour observes it. Do not remove it as dead markup.

## Files changed

| File | Change |
|---|---|
| `Mobile.MAUI/Components/Pages/Receiving/PurchaseOrder/PurchaseOrderItemView.razor` | panel reads `NSLineQuantityReceived` / `AlreadyReceived` |
| `.opencode/docs/changes/2026-10-20-po-receiving-last-scanned-qty-mismatch.md` | this note |
