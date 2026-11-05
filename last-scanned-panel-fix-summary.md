# Last Scanned Panel — behaviour reference

**Last updated:** 2026-10-20
**Status:** current. Supersedes the "pinned, never hides" decision of `9ac3fa3c`.

For the full decision history and the reasoning behind each iteration, see
`.opencode/docs/changes/2026-10-20-last-scanned-panel-scroll-behaviour.md`.

## Behaviour

The panel's visibility is a function of two inputs: **is `#recent-scanned` in view**, and
**was there a scan just now**.

| `#recent-scanned` | `LastScanned` | Panel |
|---|---|---|
| in view (operator at the top) | null | hidden |
| in view (operator at the top) | set, no recent scan | **hidden** |
| in view (operator at the top) | set, scanned < 3s ago | **visible**, auto-hides |
| out of view (operator scrolled into the list) | set | **visible**, stays |
| out of view (operator scrolled into the list) | null | hidden |

The intent: the panel never covers the heading or the first rows while the operator is at
the top, except for a short scan confirmation. Once they work down the list it follows them
and gets out of the way when they return to the top.

## How it is wired

Every item view already contains the sentinel — an empty stack at the very top:

```razor
<RadzenStack id="recent-scanned" class="box-toolbar-page"></RadzenStack>
```

and the panel itself:

```razor
<RadzenStack id="recent-scanned-floating"
             class="recent-scanned-floating"
             Visible="@(LastScanned is not null)">
```

Two calls are needed in the view:

1. `await RevealLastScanned();` right after `LastScanned = …` in every path that counts as a
   scan (barcode handler, manual entry, weight dialog).
2. `await DisposeLastScannedPanel();` in `DisposeAsync()`.

Both live in `Mobile.MAUI/Components/Base/BaseComponent.razor.cs`. Nothing else is needed:
the script import, the observer registration and the module handle are all handled lazily
inside `RevealLastScanned()`.

**Adding the panel to a new screen:** copy the two `RadzenStack` elements above, add the two
calls, and you are done. Do not add a `JsObj` field or an import block for the panel.

## Contracts to preserve

- `#recent-scanned` must keep a measurable height (`app.css` gives it `1px`). The first
  implementation observed a zero-height element with `threshold: .75` and could never fire —
  a zero-area target's ratio cannot reach 0.75.
- Do not read `window.scrollY`. Several screens scroll an inner `overflow-y: auto`
  container, and the sentinel-based observer is independent of which element scrolls.
- Never cache the panel element in a module variable. The ES module outlives the page, so a
  cached node goes stale after navigation and the panel silently stops responding.
- Hidden state is `transform: translate(-50%, -100%)`, not a fixed pixel offset, so a taller
  panel still hides completely.

## Validation

- Build: `dotnet build Mobile.MAUI/Mobile.MAUI.csproj -f net9.0-android` → 0 errors.
- No automated tests cover this; it is DOM behaviour inside the WebView. The device
  checklist is in `.opencode/docs/changes/2026-10-20-last-scanned-panel-scroll-behaviour.md`.

## Open follow-ups

- `Mobile.MAUI/wwwroot/js/PicklistIntersectionObserver.js` is dead code holding an older
  version of this logic — safe to delete.
- `Observe()` / `UnObserve()` and `#box-toolbar-floating` in
  `Mobile.MAUI/wwwroot/js/IntersectionObserver.js` are unused; two item views still import
  the module only to call `Observe()`, and their dispose path calls a `Dispose` export that
  does not exist (always throws, always swallowed). Separate cleanup.
- The 40-line panel markup is duplicated across six views; extract it into one component
  parameterised by the comparison value and the complete flag.
