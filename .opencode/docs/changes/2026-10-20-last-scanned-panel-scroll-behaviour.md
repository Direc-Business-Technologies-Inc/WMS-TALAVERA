# Last Scanned panel — hides at the top, follows the operator into the list

Date: 2026-10-20
Area: Mobile.MAUI — six item views, `BaseComponent`, `IntersectionObserver.js`, `app.css`
Branch: `fix/mob-backlogs`
Status: implemented (manual device check pending)

## Symptom

The "Last Scanned" panel was pinned permanently to the top of every item screen. On a
long list it covered the page heading and the first rows, and there was no way to get it
out of the way without leaving the screen.

## Required behaviour

- **At the top of the list** — hidden. The panel must not cover the heading or the first
  rows before the operator has started working down the list.
- **Scrolled into the list** — visible, and it stays visible.
- **Scan while at the top** — visible for a short confirmation window (~3s), then hidden
  again on its own.
- **Scan while scrolled into the list** — visible, and it does not start a timer.

## Decision history — this panel has now been changed four times

| Commit | Behaviour | Outcome |
|---|---|---|
| `c16ae69d` (initial) | `IntersectionObserver` on `#recent-scanned`, `threshold: .75`, toggling `.show-floating` | **Never fired.** A zero-height target has an intersection ratio that cannot reach 0.75, so the panel only ever appeared as a side effect of an unrelated re-render (pressing Save). |
| `06a3cc7` | Reveal on scan; hide once `window.scrollY > 80` (one-way) | Worked, but shipped two bugs: the panel element was cached in a module variable (stale after SPA navigation) and a scan re-showed it over the list while scrolled down. |
| `9ac3fa3c` | JS deleted; panel pinned permanently (`transform: translate(-50%, 0)`) | Fixed the stale-node bug by removing the mechanism. Made the panel permanently cover the top of the list. |
| this change | Sentinel-driven: hidden at the top, visible once scrolled, revealed briefly by a scan | Client-requested behaviour. |

The lesson from the first two attempts: **do not use a threshold on a zero-area target,
and do not cache a DOM node in a module that outlives the page.**

## Implementation

`Mobile.MAUI/wwwroot/js/IntersectionObserver.js`

- Observes `#recent-scanned` — the empty `RadzenStack` that already sits at the top of all
  six item views — with `threshold: 0` and reads `isIntersecting` only. This is independent
  of *which* element scrolls, which `window.scrollY` is not: `TripTicketDetailsView.razor`
  already scrolls an inner `overflow-y: auto` container.
- The observer is **state-change driven**. The first callback only records the current
  position; it must not cancel a reveal that is the reason observation started in the first
  place.
- `RevealLastScanned()` retries across animation frames (max 20, ~330ms) because the panel
  is not in the DOM until Blazor has rendered `LastScanned`. Without the retry the first
  scan of a session silently failed to show the panel.
- The panel node is re-resolved with `getElementById` on every call; the ES module instance
  survives navigation and must not hold a detached element.
- The confirmation timer is module state; it is cleared on the next scan, on a scroll state
  change, and on dispose.

`Mobile.MAUI/wwwroot/css/app.css`

- Hidden default is `transform: translate(-50%, -100%)` — height-independent, unlike the
  previous `-170px` constant, which leaked a strip of panel when a long material name
  wrapped.
- `.show-recent-scanned` restores `translate(-50%, 0)`; the 300ms transition animates
  `transform`, not `top`, so it costs no layout on a handheld.
- `#recent-scanned` gets `height: 1px` so the target has a measurable box.

`Mobile.MAUI/Components/Base/BaseComponent.razor.cs`

- `RevealLastScanned()` and `DisposeLastScannedPanel()` are shared by all six views.
- The script is imported lazily on the first scan, so no view needs a `JsObj` field, an
  import block, or a `DisposeAsync` cleanup branch for the panel.
- Both methods swallow interop failures: the panel is a convenience and must never break a
  scan.

## Verification

- `dotnet build Mobile.MAUI/Mobile.MAUI.csproj -f net9.0-android` — **0 errors**, warnings
  unchanged (902, all pre-existing).
- On device, for any Packing or Receiving item screen:
  1. Open the screen — panel is absent.
  2. Scan while at the top — panel slides down, then hides on its own after ~3s.
  3. Scroll down — panel slides down and stays.
  4. Scroll back to the top — panel hides.
  5. Scan while scrolled down — panel stays visible, no timer.
  6. Navigate to another item screen, scan — panel works there too (guards the stale-node
     regression).
  7. Open a weight/manual-entry dialog and enter a quantity — panel reveals like a scan.

## Known limits

- No automated coverage. This is DOM behaviour inside a WebView and is not reachable from
  `dotnet test`; every change here needs the device checklist above.
- The observer and its timer are module-level singletons. That is safe because only one item
  view is mounted at a time, but a future screen that shows two panels concurrently would
  need the state keyed per component.

## Files changed

| File | Change |
|---|---|
| `Mobile.MAUI/wwwroot/js/IntersectionObserver.js` | panel observer, reveal with render retry, confirmation timer |
| `Mobile.MAUI/wwwroot/css/app.css` | hidden default, show class, transform transition, sentinel height |
| `Mobile.MAUI/Components/Base/BaseComponent.razor.cs` | lazy init, `RevealLastScanned`, `DisposeLastScannedPanel` |
| `Mobile.MAUI/Components/Pages/Packing/Returns/ReturnsItemView.razor.cs` | reveal on scan, dispose |
| `Mobile.MAUI/Components/Pages/Packing/TransferOrder/TransferOrderItemView.razor.cs` | reveal on scan, dispose |
| `Mobile.MAUI/Components/Pages/Packing/VendorReturnAuthorization/VendorReturnAuthorizationItemView.razor.cs` | reveal on scan, dispose |
| `Mobile.MAUI/Components/Pages/Receiving/TransferOrder/TOxItemFulfillmentItemView.razor.cs` | reveal on scan, dispose |
| `Mobile.MAUI/Components/Pages/Receiving/Returns/TOxReturnxItemFulfillmentItemView.razor.cs` | reveal on scan, dispose |
| `Mobile.MAUI/Components/Pages/Receiving/PurchaseOrder/PurchaseOrderItemView.razor.cs` | reveal on scan and on manual entry, dispose |
