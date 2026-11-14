# TO Packing Over-Scan Fix — Guard Quantities Normalized to Display Units

**Date:** 2026-10-23
**Scope:** TO (STR) item fulfillment packing — mobile scanning guard + API save-time guard (both transports)
**Status:** Implemented, builds green, full test suite green (618 tests; one pre-existing flaky cache test unrelated to this change)

---

## 1. Summary

Operators could scan far past the planned quantity on STR TO packing. The screenshot case showed
29 boxes scanned against a planned 23; investigation showed the device would have accepted far more.
The root cause was a **unit mismatch**: the scan guard compared a quantity accumulated in the line's
DISPLAY unit (boxes) against a quantity stored in raw STOCK units (eaches), so every multi-unit line
had a guard ceiling equal to `UoMRate ×` its true remaining quantity.

The fix introduces one canonical figure — **PackableQuantity**, in display units — and uses it for
the white box, the green state, both device-side scan checks, the manual-entry cap, the refresh
restore, and both API-side guards (transform and RESTlet transports), plus a new good+bad aggregate
check and a transform-preview quantity cap.

## 2. Root cause (evidence)

SuiteQL on STR-THM00100000867, item 00032678 (SHIPPING line — the one the app reads):

```
quantity          = 888      (ABS(tl.quantity), stock units = EACHES)
rate              = 12       (unitstypeuom.conversionrate, BOX12)
backordered       = 888      (quantitybackordered — fully backordered)
packed / shipped  = 0 / 0
```

- True packable quantity: (888 − 0 − 0) / 12 = **74 boxes**
- Old guard ceiling: `LineQuantity − QuantityAlreadyShipped` = **888** — compared against a
  `ScannedQuantity` that accumulates in BOXES (barcode rate / line rate).

So the operator had 12× headroom. The 29-vs-23 screenshot was the operator stopping early; the app's
real stop was 276 scans (quantity 276 eaches = 23 boxes, guard ceiling 276).

Two aggravating facts:

1. **Fully backordered lines were scannable while hidden.** The SHIPPING line of 00032678 is fully
   backordered (backordered == quantity), so its white box was 0 and the Good list hid it
   (`NSLineQuantityPacked != 0` filter) — but barcode lookup searched the full line list, and the
   guard allowed up to 888 scans (10,656 eaches) on an invisible line.
2. **The server shared the same bug.** `FindScansExceedingRemaining` compared `ScannedQuantity`
   (boxes) against `RemainingQuantity` (eaches), so the API accepted whatever the device sent.
   `LinesClosedByBadPost` had the same mismatch in reverse (under-counted closure on multi-unit lines).

Cleared suspects: the item's three transactionline rows (ITEM / SHIPPING / RECEIVING) exist, but both
the display script and the fresh read filter `transactionlinetype = 'SHIPPING'` — line identity is
correct. `quantitypacked` was never the issue (it is 0 on this order's rows); the earlier "6 packed
units" theory is refuted by the query.

## 3. The canonical quantity

```text
PackableQuantity = (LineQuantity − max(LineQuantityPacked, QuantityAlreadyShipped) − LineQuantityBackOrdered)
                   / max(UoMRate, 1)
```

All inputs are stock units; the result is in the line's display unit — the same unit
`ScannedQuantity` accumulates in. `max(packed, shipped)` makes the plan honest on both sides: a line
already packed has nothing left to pack, while a stale/zero `quantitypacked` cannot re-open a line
that was already shipped. Backordered quantity is unavailable stock and is not packable.

Checked against the field data:

| Case | Result |
|---|---|
| 888 eaches, rate 12, nothing shipped/packed/backordered | 74 boxes — first scan past 74 blocked |
| Screenshot line: 276 eaches, rate 12 | 23 boxes — green at 23, blocked at 23 |
| Fully backordered SHIPPING line (888/888) | 0 — first scan blocked with "Over-scanning item" |
| Partially fulfilled (packed 888 eaches' worth, shipped less) | 0 — nothing left to pack |
| Stale packed (0) but shipped 40 | 40's remainder still packable |
| Missing UoMRate (0) | falls back to 1 — legacy payloads keep 1:1 behaviour |

## 4. Changes

### Device (`Mobile.MAUI`, `Shared.Libraries`)

| Where | Change |
|---|---|
| `TransferOrderLineVM.cs` | New `PackableQuantity` (formula above) and `PackedToPlan` (`ScannedQuantity >= PackableQuantity`) |
| `TransferOrderItemView.razor` | White box + Last-Scanned panel show `PackableQuantity`; green state uses `PackedToPlan`; Good list filter is `PackableQuantity > 0` (hides non-packable lines consistently) |
| `TransferOrderItemView.razor.cs` | Both scan checks (`isOverScan`, `remainingQty`) use `PackableQuantity`; Manual Entry `PlannedQty` = `PackableQuantity`; `Refresh()` clamps restored scans to the fresh plan and toasts any reduction; save payload now also forwards `LineQuantityBackOrdered` |

### API (`Integration.NS`)

| Where | Change |
|---|---|
| `NetSuiteApiClientService.PackableDisplayQuantity` (new) | The same formula on `PostTransferOrderDTO`; single source of truth server-side |
| `FindScansExceedingRemaining` | Now compares in display units (was stock units — the mobile-reported over-scan passed this guard) |
| `FindLinesExceedingRemainingAcrossBuckets` (new) | Good + bad scans of the SAME line live in different payload rows; each row passed the per-row check while their sum exceeded the cap. Flags the combined total with a GOOD/BAD breakdown |
| `EnsureScansWithinPackableQuantity` (new) | Shared gate (per-row + aggregate) used by **both** transports |
| Transform path + RESTlet path | Both call the shared gate (previously each had its own copy of the un-normalized check) |
| `VerifyTOFulfillmentAgainstTransform` | New quantity cap: a scan above the preview line's own `quantity` — the amount NetSuite would fulfil if the line went unaddressed — is rejected by name. Active when `WMS_TO_IF_TRANSFORM_VERIFICATION` is on (default off; verify the preview quantity unit on a sandbox order before enabling) |
| `GetFreshTOFulfillmentLineStateAsync` | Fresh read now also carries `quantitypacked` and `quantitybackordered` |
| `ReconcileTOFulfillmentLines` / `MergeFreshTOFulfillmentLineState` | Overwrite the posted rows' packed/backordered (and fillers get them), so the guards run on CURRENT state, not the device's load-time snapshot |
| `LinesClosedByBadPost` | Closure comparison normalized to display units (under-counted closure on multi-unit lines) |

Tolerance: `ScanQuantityTolerance = 0.00001m` — matches the device's 5-decimal scan rounding.

## 5. Tests

New/updated (all in `Integration.NS.Tests`):

- `TransferOrderLineVMPackableQuantityTests` (new file, new `Shared.Libraries` project reference in the test csproj): 888→74 conversion, packed/shipped/backordered consumption, rate fallback, green state, zero-plan refusal.
- `NetSuiteApiClientServiceClosedLineGuardTests`: the field case (74 ok / 75 flagged / 888 flagged), fully-backordered refusal, packed consumption, shipped-when-packed-stale, rate fallback, good+bad aggregate (25 of 23 flagged, 23 ok), save-gate throw + pass cases.
- `NetSuiteApiClientServiceTOFulfillmentTransformVerificationTests`: scan beyond preview quantity rejected by name; at-quantity kept; absent preview quantity fails open. Existing fixtures updated to carry consistent quantities (the cap is now enforced).

Result: 618 tests, 617 passing. `GridResultCacheTests.Key_index_does_not_outlive_the_cache_it_tracks`
fails intermittently under parallel load and passes in isolation — a pre-existing flaky test,
unrelated to this change (also seen failing/passing before it).

## 6. Behaviour changes to expect in the field

- On a rate-12 line planned for 23 boxes, scanning stops at 23 (previously ~276). Operators lose the
  phantom headroom; expect a few "Over-scanning item" toasts in the first days — that is the bug
  being fixed.
- Fully backordered items now refuse scans (they were invisible in the list but scannable before).
- If the plan shrinks while an operator has the page open, Refresh clamps their scans to the fresh
  plan and lists what was reduced.

## 7. Deployment

1. **API first** — the API alone (with the existing APK) starts refusing over-scans at save time,
   which is the safety net. Confirmed flags in the startup log: `reconciliation`, `failClosed`,
   `restlet`, `transformVerify`.
2. **APK second** — the device guard then blocks at scan time, so the operator is told before
   packing, not after.
3. Optionally enable `WMS_TO_IF_TRANSFORM_VERIFICATION` after observing one sandbox order, which
   adds the preview-quantity cap as the authoritative belt-and-braces check.

## 8. Remaining risks

- The preview-quantity cap assumes the transform preview's `quantity` is in the line's posting unit
  (the same unit the POST consumes). It is gated behind the default-off flag precisely so this can be
  verified against a real preview before enabling.
- NetSuite's own over-fulfillment configuration remains the deeper brake; the app now matches it
  instead of pre-empting it with a 12×-too-lax number.
- The Done tab still shows `QuantityAlreadyShipped` in raw stock units; cosmetic only.
