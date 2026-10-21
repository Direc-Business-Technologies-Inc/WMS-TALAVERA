# Mobile Good/Bad Scan Toggle — Floating Helper Fix

Problem: on mobile, the floating scan-actions Good/Bad button alternated between
Good and Bad on its own as the operator scanned, instead of being operator-toggled.
Observed on the Inventory Worksheet; the same wiring existed on every scan screen.

## Root cause

The app never auto-toggled — `ToggleScanState()` was wired only to the button's
`Click`. The toggle stimulus was external:

- The device scanners were configured (or misconfigured) with **keystroke output**
  enabled alongside intent/broadcast output. A scan therefore typed the barcode
  plus a trailing **Enter** into the `BlazorWebView`.
- Tapping a button in a WebView leaves it **focused**. `Enter` on a focused
  `<button>` firex its click event, so every scan re-activated whatever scan-action
  button the operator last tapped.
- The 3-state cycle (`Base → Good → Bad → Base`) made each scan step the button
  one state, which read as "alternating between Good and Bad".

Corroboration: the web app guards against exactly this in
`Web.BlazorServer/wwwroot/js/custom-scripts/scanner.js:29-45` (global keypress
buffering); the mobile app had no equivalent.

## Fix 1 — keyboard guard (root cause defense)

`Mobile.MAUI/wwwroot/js/keyboard-guard.js`, loaded from
`Mobile.MAUI/wwwroot/index.html:38`.

- Capture-phase `document` keydown listener: `preventDefault()` when the key is
  `Enter`/`Space` and the focused element is a `<button>`. Scanner keystrokes can
  no longer activate any button on any screen. Touch clicks are unaffected.
- `window.blurActiveElement` helper used by the toggle component (below).

Note: this does not touch keystrokes into inputs/dropdowns, so keyboard-driven
device configs still work for dropdown/manual entry.

## Fix 2 — centralized `ScanToggleButton` component

`Mobile.MAUI/Components/Reusables/ScanToggleButton.razor`

All seven screens previously carried a copy-pasted `#region Button States`
(icon/label/style switches + `ToggleScanState()`, each ~45 lines). The component
now owns:

- The state cycle: `Base → Good → Bad → Base` (uniform across screens).
- Icon (`check`/`block`), label (`Good`/`Bad`), and button color
  (`Base`/`Success`/`Danger`).
- A post-toggle `blurActiveElement()` call so no stale focus can be re-triggered.

Usage:

```razor
<ScanToggleButton @bind-ScanState="ScanState" Disabled="@ActionSaveScan.Busy" />
```

Migrated screens (razor block replaced by the one-liner; code-behind collapsed to
`ScanState` + a computed property):

| Screen | File |
|---|---|
| Inventory Worksheet | `Mobile.MAUI/Components/Pages/InventoryCounting/InventoryWorksheet/CreateInventoryWorksheetView.razor(.cs)` |
| Inventory Counting Item | `Mobile.MAUI/Components/Pages/InventoryCounting/InventoryCountingItemView.razor(.cs)` |
| PO Receiving Item | `Mobile.MAUI/Components/Pages/Receiving/PurchaseOrder/PurchaseOrderItemView.razor(.cs)` |
| STR Receiving (TO x IF) Item | `Mobile.MAUI/Components/Pages/Receiving/TransferOrder/TOxItemFulfillmentItemView.razor(.cs)` |
| Receiving TO Item | `Mobile.MAUI/Components/Pages/Receiving/TransferOrder/TransferOrderItemView.razor(.cs)` |
| Packing TO Item | `Mobile.MAUI/Components/Pages/Packing/TransferOrder/TransferOrderItemView.razor(.cs)` |
| Packing VRA Item | `Mobile.MAUI/Components/Pages/Packing/VendorReturnAuthorization/VendorReturnAuthorizationItemView.razor(.cs)` |

State contract: `ScanState` stays in the page; `NextScanIsBad` is now a computed
property (`ScanState == ToggleState.Bad`) so the two can never drift apart.

## Fix 3 — PO Receiving cycle slip (copy-paste, now moot)

`PurchaseOrderItemView.razor.cs:1295` (pre-fix) mapped `Bad → Good`, so once the
operator left `Base` the button cycled `Good ↔ Bad` forever. Every other screen
returned to `Base`. Centralizing the cycle in the component removes the
divergence.

Behavior note for PO operators: with the standard cycle, tapping three times
returns to `Base`, and `Base` **blocks scans** in PO
(`HandleItemScan` gate, `PurchaseOrderItemView.razor.cs:437`) until the button is
tapped again. This matches the other screens; confirm ops are aware.

## Cleanup included

- Removed 4 dead `ToggleQuality()` methods (flipped `NextScanIsBad` directly;
  not referenced by any razor file).
- Minor visual change: PO's toolbar used `Gap="5px"` vs the standard `4px`; now
  unified at 4px.
- Net diff: −429 lines / +42 lines.

## Known gaps (not addressed)

- **Device configuration** — keystroke output should be disabled in the
  scanner/DataWedge profile (keep intent/broadcast output only). The guard is
  defense, not a substitute for correct device config.
- **Duplicate scan receivers** — `ScannerReceiver`
  (`Mobile.MAUI/Platforms/Android/Helper/ScanReceiver.cs:6-8`) and
  `TestBroadcastReceiver` (`Mobile.MAUI/Platforms/Android/MainActivity.cs:31-32`)
  both listen on `com.wmsmobile.appname.scan`.
- **Dead scan path** — `Mobile.MAUI/Services/ScanService.cs` publishes
  `OnScanned` but nothing subscribes.
- **Screen-level Base gating** — the Worksheet records scans in `Base` while the
  other gated screens ignore them (`HandleScan`/`HandleItemScan` gates). Pre-existing
  per-screen policy, unchanged.

## Verification checklist (device)

1. Tap Good/Bad once (Base → Good), then scan with the trigger: button must stay
   on Good; the scan lands in the Good tab.
2. Tap again (Good → Bad), scan: quantity lands in the Bad tab; button stays Bad
   across scans.
3. Third tap returns to Base; on gated screens scans are ignored until re-toggled.
4. Confirm the scanner profile: keystroke output disabled, intent output
   targeting `com.wmsmobile.appname.scan`.
5. Regression: dropdown/manual-entry keyboard use still works; Move/Remove
   buttons still toggle by touch.
