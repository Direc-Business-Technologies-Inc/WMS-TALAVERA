# Last Scanned Panel Fix — Summary

**Date:** 2026-10-19
**Branch:** *not created yet* — see Notes
**Issue:** On mobile Packing, the "Last Scanned" panel did not appear when an item was scanned. It stayed hidden until **Save** was pressed, and when it did appear it slid down slowly.
**Final behaviour (per client):** the panel is pinned at the top and stays visible. It must never hide on its own.

## Root causes

Two separate bugs, both in our code. **Not** a network or connection issue — nothing in this path makes a server call.

1. **The panel was watching the wrong thing.** Instead of showing on scan, it was driven by an `IntersectionObserver` watching `#recent-scanned` — an empty placeholder element carrying a CSS class (`box-toolbar-page`) that no longer existed in `app.css`. With no size and no content, the panel only ever moved as a *side effect* of something else re-rendering the page. Pressing Save reshuffles the item list, which is why it appeared then, by accident.
2. **The slide-down animation was expensive.** The panel animated its `top` position. `top` forces the browser to re-lay-out the whole page on every frame. On a handheld with a long item list, the 300ms slide visibly stuttered (clearly visible in the client screenshots).

## Fix

The client asked for the panel to be simply pinned at the top, so the whole show/hide mechanism was deleted rather than patched:

- **Removed the JavaScript entirely.** `ObserveRecentScanned`, `ShowLastScanned`, `ObserveLastScannedScroll` and `UnObserveLastScannedScroll` are gone, along with the module state they needed. The broken `IntersectionObserver` trigger no longer exists.
- **Panel is now pure CSS.** `.recent-scanned-floating` is fixed at the top and never moves. No animation, so the expensive reflow is gone by definition.
- **Visibility is a plain Blazor binding.** The panel renders only when there is a last-scanned item (`Visible="@(LastScanned is not null)"`). It appears the instant you scan and then simply stays put.
- **Removed the now-dead plumbing** this exposed: the `JsObj` JS handle, the `IntersectionObserver.js` import, and the `DisposeAsync` cleanup block, in all six views. The panel needs no JavaScript at all, so the per-scan interop call is gone too — scanning is now pure C# with no JS round-trip.

## Files changed

**Shared**
- `Mobile.MAUI/wwwroot/css/app.css`
- `Mobile.MAUI/wwwroot/js/IntersectionObserver.js` (panel logic removed)
- `Mobile.MAUI/Components/Base/BaseComponent.razor.cs` (reverted to original — no helpers needed)

**Views** (`.razor` markup + `.razor.cs` plumbing removed from each)
- `Mobile.MAUI/Components/Pages/Packing/Returns/ReturnsItemView`
- `Mobile.MAUI/Components/Pages/Packing/TransferOrder/TransferOrderItemView`
- `Mobile.MAUI/Components/Pages/Packing/VendorReturnAuthorization/VendorReturnAuthorizationItemView`
- `Mobile.MAUI/Components/Pages/Receiving/TransferOrder/TOxItemFulfillmentItemView`
- `Mobile.MAUI/Components/Pages/Receiving/Returns/TOxReturnxItemFulfillmentItemView`
- `Mobile.MAUI/Components/Pages/Receiving/PurchaseOrder/PurchaseOrderItemView`

Net effect: **277 lines deleted, 7 added.**

## Validation

- Full solution build: **0 errors**. Warning count dropped 2754 → 2724 (dead code removed).
- **Not yet verified on a real handheld.** Please confirm: panel is absent before the first scan, appears immediately on the first scan, and stays pinned while scrolling.

## Notes / follow-ups

- `Mobile.MAUI/wwwroot/js/PicklistIntersectionObserver.js` is **dead code** — nothing imports it, and it holds an older conflicting version of this logic. Safe to delete; needs removing manually (no delete tool was available).
- `Receiving/TransferOrder/TransferOrderItemView.razor` and `Receiving/Returns/ReturnsItemView.razor` still have their panel markup commented out. Untouched — separate decision.
- The empty `#recent-scanned` placeholder still exists in ~10 Razor files and is now unused. Cleanup candidate.
- No branch or commit yet — this was written without one.