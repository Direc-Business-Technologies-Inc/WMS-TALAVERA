# TO Item Fulfillment Freshness Revalidation (Stale-Snapshot Static-Sublist Fix)

Date: 2026-10-21
Status: Implemented (TO STR packing path)
Mode: Feature / Fortification
Scope: `SaveTOItemFulfillment` in `Integration.NS/Services/NetSuiteApiClientService.cs`
Related: [IF replace-param / INVALID_PARAMETER fix](2026-10-15-if-replace-param-invalid-parameter-fix.md); [PO closed-line guard fix](2026-10-17-po-item-receipt-closed-line-guard-fix.md); [Web STR closed-line guard fix](2026-10-19-web-str-packing-closed-line-guard-missing-fix.md); [NetSuite 4xx catalog](../.opencode/docs/mobile-netsuite-4xx-catalog.md) case 6b

## Summary

Field report 2026-10-21: a GOOD TO Item Fulfillment post failed with

```
[USER_ERROR] Error while accessing a resource. You have attempted an invalid
sublist or line item operation. You are either trying to access a field on a
non-existent line or you are trying to add or remove lines from a static
sublist. [diag replace=True usedBin=True scannedLines=2 totalLines=714]
```

The diag rules out the payload-shape and `?replace=` regressions (both correct).
The root cause is **stale line state**: every posting guard runs on the line
snapshot the device loaded. On a 714-line order that snapshot can be hours old;
a line fulfilled elsewhere or closed in the window is absent from the
transform's item sublist, and any `orderLine` reference for it - a scanned entry
or a zero-qty filler - is an invalid insert on a static sublist that fails the
whole save. The earlier failed BAD post makes this *expected*: it ships lines
before the GOOD post ever runs.

## Corrective Direction (what was chosen)

**Server-side freshness revalidation at save time.** Before the closed-line
strip and the over-remaining guard run, `SaveTOItemFulfillment` now re-reads the
order's current SHIPPING-line state straight from NetSuite and merges the fresh
figures into the posted DTO rows.

Design choice that keeps every prior fix intact: the merge **overwrites the
snapshot fields in place** (`LineQuantity`, `QuantityAlreadyShipped`, `IsClosed`)
and does **not** change any guard logic. The existing closed-line strip
(`IsClosedFulfillmentLine`), the over-remaining check
(`FindScansExceedingRemaining`), the dedupe, and the missing-bin guard all keep
their verified behaviour - they simply now judge CURRENT state instead of the
device's load-time snapshot.

## Implementation

New members in `NetSuiteApiClientService`:

| Member | Role |
|---|---|
| `TOFulfillmentFreshLineState` | Fresh per-line state: `LineId` (the WMS `LineSequenceNumber`, = `transactionline.id`), `LineQuantity` (`ABS(tl.quantity)`), `QuantityShipped` (`NVL(tl.quantityshiprecv,0)`), `IsClosed` (`tl.isclosed`). Raw stock units, mirroring the load script `NS_TO_x_Packing_Get_Items` so the two are directly comparable. |
| `GetFreshTOFulfillmentLineStateAsync(orderId)` | One SuiteQL query over `transactionline` joined to `transaction`, filtered to `recordtype IN ('transferorder','intercompanytransferorder')` and `transactionlinetype = 'SHIPPING'`, keyed on the order internal id. |
| `MergeFreshTOFulfillmentLineState(lines, freshLines)` | Static and unit-testable. Overwrites each posted row's figures with fresh values; marks lines **absent** from the fresh set as closed (the same condition NetSuite reports as the static-sublist 400 - the existing strip already removes those). Returns descriptors for scanned lines that went closed or fully shipped. |

Wiring in `SaveTOItemFulfillment`, immediately after the dedupe and before the
closed-line strip:

1. Revalidate (query + merge) inside a `try` that **fails open**: if the
   revalidation query itself fails, the save logs a warning and proceeds on the
   snapshot exactly as before. The transform post remains the final arbiter, and
   the revalidation adds no new failure mode to previously successful saves.
2. If any **scanned** line was closed or fully shipped since the snapshot, the
   save fails loud with an actionable message:

```
Cannot post TO Item Fulfillment: the following scanned line(s) were fulfilled
or closed after this order was loaded: WMS line 536 (00018051 - ITEM NAME):
scanned 50, already shipped 200 of 200. Reload the order and rescan the
affected line(s).
```

## Trade-offs

- **Fail loud on newly-closed scans, silent on newly-closed fillers.** A closed
  filler is dropped by the strip with no operator impact. A closed *scan* means
  the operator physically holds goods that can no longer be fulfilled on this
  order - silently skipping the line would strand stock with no message. This
  deliberately differs from the PO receipt closed-line guard (which strips
  scanned-but-closed lines and only errors when nothing postable remains); for
  TO packing the physical-goods consequence is the one worth stopping for.
- **One extra SuiteQL round-trip per save.** Negligible against the transform
  call itself, and the query is scoped to a single order (the per-TO aggregate
  scoping pattern from `d05bc6b5`).
- **Fail-open on revalidation failure.** Availability of the save is not
  reduced; the stale-snapshot failure can still occur in that path, but only in
  the same shape as before, with the diag suffix to diagnose it.
- **Not rolled into Returns / VRA yet.** `SaveReturnsItemFulfillment` and
  `SaveVRAItemFulfillment` share the staleness exposure; the merge helper is
  written against `PostTransferOrderDTO`-shaped rows so the follow-up is a
  wiring change, not a redesign.

## Deliberately NOT built

- **Optimistic concurrency / version tokens** - NetSuite's transform endpoint
  has no ETag-style support; inventing a protocol here is unjustified.
- **Automatic retry** - the BAD-then-GOOD posting sequence has no compensation;
  a retry loop on top of a partially-fulfilled TO risks duplicate fulfillments.
  Reconcile in NetSuite, reload, rescan.
- **Revalidation on every scan** - only the save moment matters.

## Tests

`Integration.NS.Tests/Services/NetSuiteApiClientServiceFreshnessRevalidationTests.cs`
(mirrors the closed-line guard tests' CP style):

| CP | Assertion |
|---|---|
| CP-1 | Fresh shipped quantity overwrites the snapshot; the over-remaining guard then flags a scan that the snapshot believed valid. |
| CP-2 | A line absent from the fresh set is marked closed and removed by the existing strip. |
| CP-3 | A scanned line that went closed *or* fully shipped is reported, naming WMS line, material, scanned and fresh figures. |
| CP-3b | A newly-closed filler is NOT reported (routine, silently stripped). |
| CP-4 | Unchanged open lines pass through; no new failure mode. |
| CP-5 | Scan equal to fresh remaining still allowed; the merge does not tighten the limit. |
| CP-6 | A stale closed flag is corrected by fresh open state; fresh state is the single source of truth. |

Result: 26/26 across the freshness, closed-line, dedupe and diagnostics suites;
480/481 on the full `Integration.NS.Tests` run. The single failure
(`NetSuiteApiClientServiceTokenTests.HttpClient_has_explicit_timeout_of_100_seconds_by_default`)
is pre-existing env-var test pollution - it passes in isolation and is untouched
by this change.

## Validation Criteria

- Reproduce the field scenario: load a large TO on the device, fulfill/close a
  line from another session (or let the BAD post ship it), save on the device
  without reloading. Expected: the actionable "fulfilled or closed after this
  order was loaded" message, NOT the NetSuite static-sublist 400.
- Re-scan after reload: the save posts and the IF quantities match NetSuite's
  open quantities.
- Ordinary single-session save: behaviour identical to before, plus one
  SuiteQL query in the server log.
- NetSuite SuiteQL down / erroring at save time: save proceeds on the snapshot
  (warning logged), no new user-facing failure.

## Follow-ups

1. Wire the same revalidation into `SaveReturnsItemFulfillment` and
   `SaveVRAItemFulfillment` (shared helper; per-DTO key selectors).
2. Compensate or reconcile the BAD-then-GOOD non-atomicity so a mid-sequence
   failure does not leave a partially fulfilled TO (the other half of the
   2026-10-21 field report).
3. Fix the pre-existing env-var pollution in the token/timeout tests so the
   full suite is green deterministically.
