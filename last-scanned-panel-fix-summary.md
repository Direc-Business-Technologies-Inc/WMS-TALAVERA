# Last Scanned Panel Fix — Summary

**Date:** 2026-10-19
**Branch:** *not created yet* — see Notes
**Issue:** On mobile Packing, the "Last Scanned" panel did not appear when an item was scanned. It stayed hidden until **Save** was pressed, and when it did appear it slid down slowly.

## Root causes

Two separate bugs, both in our code. **Not** a network or connection issue — nothing in this path makes a server call.

1. **The panel was watching the wrong thing.** Instead of showing on scan, it was driven by an `IntersectionObserver` watching `#recent-scanned` — an empty placeholder element carrying a CSS class (`box-toolbar-page`) that no longer existed in `app.css`. With no size and no content, the panel only ever moved as a *side effect* of something else re-rendering the page. Pressing Save reshuffles the item list, which is why it appeared then, by accident.
2. **The slide-down animation was expensive.** The panel animated its `top` position. `top` forces the browser to re-lay-out the whole page on every frame. On a handheld with a long item list, the 300ms slide visibly stuttered (clearly visible in the client screenshots).

## Fix

- **Shows on scan.** The scan handler now calls `RevealLastScanned()` the moment an item is scanned, so the panel drops down immediately.
- **Hides on scroll.** A passive scroll listener hides it again once the user scrolls past 80px, so it never sits on top of the item list.
- **Smooth animation.** The panel now animates `transform` instead of `top`. Transforms are handled by the graphics card and cost no layout, so the slide is smooth on cheap devices.
- **Shared logic.** Implemented once in `BaseComponent` instead of copy-pasted into six pages.

## Files changed

**Shared**
- `Mobile.MAUI/Components/Base/BaseComponent.razor.cs`
- `Mobile.MAUI/wwwroot/js/IntersectionObserver.js`
- `Mobile.MAUI/wwwroot/css/app.css`

**Views wired up** (all had the same bug)
- `Mobile.MAUI/Components/Pages/Packing/Returns/ReturnsItemView.razor.cs`
- `Mobile.MAUI/Components/Pages/Packing/TransferOrder/TransferOrderItemView.razor.cs`
- `Mobile.MAUI/Components/Pages/Packing/VendorReturnAuthorization/VendorReturnAuthorizationItemView.razor.cs`
- `Mobile.MAUI/Components/Pages/Receiving/TransferOrder/TOxItemFulfillmentItemView.razor.cs`
- `Mobile.MAUI/Components/Pages/Receiving/Returns/TOxReturnxItemFulfillmentItemView.razor.cs`
- `Mobile.MAUI/Components/Pages/Receiving/PurchaseOrder/PurchaseOrderItemView.razor.cs`

## Validation

- Full solution build: **0 errors** (warnings unchanged from before the change).
- **Not yet verified on a real handheld.** Please confirm: scan shows the panel immediately; scrolling down hides it; the slide is smooth.

## Behaviour change

The panel is now **one-way per scan**: scan → shows, scroll down → hides, and it stays hidden until the next scan. It does **not** come back when you scroll up. This was an intentional choice; it can be changed to a two-way toggle if operators expect the panel to return when scrolling back to the top.

## Notes / follow-ups

- `Mobile.MAUI/wwwroot/js/PicklistIntersectionObserver.js` is **dead code** — nothing imports it, and it holds a third conflicting version of this logic. Safe to delete.
- `.box-toolbar-floating` / `.show-floating` and `Observe()` in the JS are also unused (no page declares `id="box-toolbar-floating"`). Left untouched to keep this change focused.
- The empty `#recent-scanned` placeholder still exists in ~10 Razor files and is now unused. Cleanup candidate.
- No branch or commit yet — this was written without one.