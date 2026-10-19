# Fix: Web STR Packing Item Receipt ignores closed lines (missing guard fields)

Date: 2026-10-19
Status: Fix in source, build clean (Web.BlazorServer + Api.CoreWebAPI), 423/423 tests green — **client-verified 2026-10-19: the original failing TO now saves from the web STR Item Receipt page**
Mode: Bugfix — web counterpart of the mobile TO closed-line guard
Scope: Packing module — web STR Item Receipt (`PackingItemReceiptCreatePage` → `ItemReceiptPackingHandler` → `PostTransferOrderIFCmd` → `SaveTOItemFulfillment` → `transferOrder/{id}/!transform/itemFulfillment`), plus the shared TO fulfillment diagnostics decoder
Related:
- `NetSuiteApiClientServiceClosedLineGuardTests` (Integration.NS.Tests) — the server-side TO guard this fix re-arms for the web path. Its docstring records the identical failure mode on the mobile path (field failure 2026-10-16, ISTR-THM00100000631).
- [2026-10-17 PO Item Receipt closed-line guard fix](2026-10-17-po-item-receipt-closed-line-guard-fix.md) — the sibling fix for the PO receipt transform; the same "closed line has no sublist line to address" mechanism.
- Commit `cb4b0987` (mobile TO packing closed-line handling) and `1cd05c57` (PO item receipt, the pending deploy) — see *Why the pending PO deploy could not clear it*.
- `Mobile.MAUI` `TransferOrderItemView.razor.cs` + `Shared.Libraries/ViewModel/TransferOrder/TransferOrderLineVM.cs` — the mobile path that already carries the two fields this fix adds to the web path.

Changed files:
- `Web.BlazorServer/Handlers/Implementations/Transaction/Packing/ItemReceipt/ItemReceiptPackingHandler.cs` — remaining is now `LineQuantity − QuantityAlreadyShipped`; closed / fully-shipped lines excluded from the payload; `IsClosed` and `QuantityAlreadyShipped` copied onto the DTO
- `Integration.NS/Services/NetSuiteApiClientService.cs` — new `DecodeTOItemFulfillmentLine`; the GOOD/BAD `itemFulfillment` catch blocks use it
- `Integration.NS.Tests/Services/NetSuiteApiClientServiceTOFulfillmentDiagnosticsTests.cs` (new, 5 cases)

## Summary

Fulfilling a **partially fulfilled** transfer order from the web STR Item Receipt page fails:

```
Fulfill Packing Item failed: Error while posting GOOD TO Item Fulfillment.
NetSuite 400 Bad Request: Bad Request(400): [USER_ERROR] Error while accessing a resource.
Please configure the inventory detail for DEL MONTE P/A SLICED 1.5TINS 432G.
[diag replace=True usedBin=True scannedLines=3 totalLines=561]
```

(field screenshot, 2026-10-18). Same server method as the mobile packing path, same NetSuite text family — but reached through a **web-only gap** that the mobile fix and the pending PO deploy do not touch.

Web and mobile converge on `SaveTOItemFulfillment`, whose closed-line defence reads two `PostTransferOrderDTO` fields:

```csharp
// NetSuiteApiClientService.cs:1371
internal static bool IsClosedFulfillmentLine(PostTransferOrderDTO line) =>
    line.IsLineClosed || line.RemainingQuantity <= 0;
```

Neither is populated by the web handler, so on the web path the guard is inert: closed lines survive the pre-filter and are emitted as `orderLine` references (and zero-qty fillers) for lines the transform's item sublist no longer contains — an invalid insert on a static sublist, which NetSuite rejects with the text above. `totalLines=561` against `scannedLines=3` is the tell: the payload carried nearly the whole order, almost all of it filler.

## Root Cause

### 1. The web handler never set the two fields the server guard reads

`ItemReceiptPackingHandler.MapToDto` populated `LineQuantity`, `LineQuantityPacked`, `LineQuantityBackOrdered`, `NSLineQuantity*`… but **neither `IsClosed` nor `QuantityAlreadyShipped`** — even though the query projects both (`NS_TO_x_Packing_Get_Items.sql:44-45`) and the source DTO exposes both (`Application.DataTransferObjects/Transactions/Commons/NS/TransferOrderLineDTO.cs:29,35`).

Consequence, exactly:

- `PostTransferOrderDTO.RemainingQuantity => LineQuantity − QuantityAlreadyShipped` (`PostTransferOrderDTO.cs:49`) becomes `LineQuantity − 0`.
- `IsClosedFulfillmentLine` is therefore `false` for every web line.
- The strip at `NetSuiteApiClientService.cs:1443` is a no-op on the web path, and `FindScansExceedingRemaining` (`:1382`, called `:1449`) can never fire — remaining is inflated.

### 2. The web pre-filter used the packed-based remaining

`ItemReceiptPackingHandler.cs:51` filtered with `GetRemainingQuantity(qty, backOrdered, packed, rate)` = `max(0, (qty − (backOrdered + packed)) / rate)` — the quantitypacked-based figure. On a partially fulfilled order this overstates the open quantity (packed is not what has already shipped), so closed lines passed the filter. The filler at `:65-68` then added a GOOD copy for **every** surviving line (`goodQty = 0 < quantityOpen` → `true`), which is why `totalLines` was so large.

Two filters (web pre-filter + server guard) were meant to exclude closed lines; on the web both were ineffective because they disagreed about what "remaining" means and one of them was never fed.

### Why the pending PO deploy could not clear it

`1cd05c57` touches only: `PurchaseOrderIRPayloadDTO`, its tests, `NS_PurchaseOrder_Get_Items.sql`, `PurchaseOrderItemView.razor.cs`, and the **PO regions** of `NetSuiteApiClientService.cs` (`BuildStamp`, `FindPOMissingBinLines`, `DecodeItemReceiptLine`, PO diag lines). No web handler, no TO payload. A deploy cannot change this behaviour; the web path required a handler change.

## How it was found (investigation path)

1. **Pinned the web flow.** `PackingItemReceiptCreatePage.razor.cs:16,71` → `ItemReceiptPackingHandler.PostItemFulfillment` (`:41`) → `PostTransferOrderIFCmd` (`:49`) → `SaveTOItemFulfillment`. Same query as mobile (`NS_TO_x_Packing_Get_Items.sql`).
2. **Compared the two producer paths.** The mobile view copies both fields (`TransferOrderItemView.razor.cs:133-134, 185-186, 232-233, 684-685`) and routes completed lines to a read-only list (`:200`); the web handler copies neither.
3. **Confirmed the guard was inert on web.** With `IsClosed=""` and `QuantityAlreadyShipped=0`, both clauses of `IsClosedFulfillmentLine` are false for every line.
4. **Confirmed the deploy scope.** `git log -1 --stat 1cd05c57` — PO-only.
5. **Confirmed the diagnostics blind spot.** The toast uses the `…inventory detail for <item>.` phrasing; both TO decoders matched only `@"line (\d+) of the item list"` (`:1477`, `:1528`), so no `lineN=` suffix is emitted — exactly what the toast shows.

## The Fix

### 1. Remaining is `LineQuantity − QuantityAlreadyShipped`

`ItemReceiptPackingHandler.cs` `:56`, `:86`, `:117` and the helper `:172-173`:

```csharp
private static decimal GetRemainingQuantity(decimal quantity, decimal alreadyShippedQuantity, decimal uoMRate) =>
    Math.Max(0, ConvertQuantity(quantity - alreadyShippedQuantity, uoMRate));
```

This is the same baseline `PostTransferOrderDTO.RemainingQuantity` and the mobile `TransferOrderLineVM.NSLineQuantityRemaining` (`= LineQuantity − QuantityAlreadyShipped`) use.

### 2. Closed / fully-shipped lines are excluded before mapping

`ItemReceiptPackingHandler.cs:51` replaces the packed-based `> 0` filter with `!IsCompleted(x)`, where `:178-179`:

```csharp
private static bool IsCompleted(TransferOrderLineDTO line) =>
    IsLineClosed(line) || GetRemainingQuantity(line.LineQuantity, line.QuantityAlreadyShipped, line.UoMRate) <= 0;
```

A closed line is now dropped before any `PostTransferOrderDTO` is built, so no `orderLine` reference (scanned or filler) is emitted for it. Mirrors `TransferOrderLineVM.IsCompleted`.

### 3. The two guard fields are carried onto the DTO

`ItemReceiptPackingHandler.cs:149-150`:

```csharp
IsClosed = dto.IsClosed,
QuantityAlreadyShipped = dto.QuantityAlreadyShipped,
```

Defence-in-depth: re-arms the server strip (`:1443`) and the over-scan guard (`:1449`) for a line that a stale client, or the manual-close case below, still submits.

### 4. The item-named inventory-detail variant is decoded

`NetSuiteApiClientService.cs:1412` new `DecodeTOItemFulfillmentLine` handles both phrasings — `line N of the item list` (unchanged, index into the posted bucket) and `inventory detail for <item>.` (resolved by `MaterialName` / `MaterialCode`). The capture runs to end-of-message and is trimmed of the trailing sentence period, because an item name can itself contain a dot (`SLICED 1.5TINS`). Both the GOOD (`:1551`) and BAD (`:1507`) catch blocks use it.

### Why the backorder term was dropped

The old remaining subtracted backordered **and** packed; the new one subtracts only `quantityshiprecv`. Backorder was not removed as a concept — it is still projected by the query, displayed (`MapToVm` `QuantityBackOrdered`), and carried in the payload (`MapToDto` `LineQuantityBackOrdered`). It was removed **only from the open-quantity formula**, so that the web agrees with the two authorities that already define "remaining":

- the mobile view — `NSLineQuantityRemaining => LineQuantity − QuantityAlreadyShipped`;
- the server guard — `PostTransferOrderDTO.RemainingQuantity => LineQuantity − QuantityAlreadyShipped`.

Keeping a third, packed+backorder-based definition on the web is precisely the disagreement that let closed lines through. A side effect is that on a fresh order the web now reports the backordered quantity as open (the new value is larger by the backordered amount), matching the mobile screen. If the business decides backordered quantity must **not** be packable, that is a separate change that must alter the mobile VM **and** the server guard together — changing only the web handler would reintroduce the disagreement.

## Behaviour Matrix (current code)

| Line state | Web pre-filter | Payload | Server guard (now armed) |
|---|---|---|---|
| Closed (`isclosed = T`) | dropped by `!IsCompleted` | absent (no scan, no filler) | would strip if it arrived |
| Fully shipped (`quantityshiprecv = LineQuantity`) | dropped | absent | would strip if it arrived |
| Open, scanned ≤ remaining | kept | posted as scan | allow |
| Open, scanned > remaining | kept | posted as scan | **blocked** — "scanned quantity exceeds the remaining quantity for: item-list line …" |
| Open, unscanned (filler) | kept | zero-qty filler | ignore (zero scan) |

## Tests

**New:** `Integration.NS.Tests/Services/NetSuiteApiClientServiceTOFulfillmentDiagnosticsTests.cs` — 5 cases:

- `line N of the item list` still names the payload row
- `inventory detail for <item>.` resolves by `MaterialName`, with a dotted name (`DEL MONTE P/A SLICED 1.5TINS 432G`)
- the same variant resolves by `MaterialCode`
- an unmatched item name is reported as unmatched (not silently dropped)
- a message with neither marker returns empty

**Existing and still green:** `NetSuiteApiClientServiceClosedLineGuardTests` (the server-side TO guard the fix now feeds). Total suite **423/423 passed**.

Build: `dotnet build Web.BlazorServer` and `dotnet build Api.CoreWebAPI` → **0 errors** (pre-existing warnings only).
Graph: `graphify update .` → `18649 nodes, 43964 edges, 664 communities`.

## Verification Plan

Must be confirmed on the client; no NetSuite sandbox was available in this session.

1. **The original failing TO.** Reload and re-attempt must no longer post closed-line fillers. Confirm the STR Item Receipt page saves. If a scan exceeds the true remaining, the operator should now see the item-list/WMS-line message, not a raw 400.
2. **An open TO at a bin-tracked location.** Must post exactly as before — this fix must not touch the working path.
3. **A partially fulfilled TO.** Scan ≤ remaining posts; the closed lines are absent from the posted body.
4. **Diagnostics.** On a forced failure, the toast should now carry an `item=…` suffix naming the offending item even without the `line N` marker.

The behavioural check that matters most is (1): destructure the logged payload and confirm no `orderLine` for a closed line survives into the GOOD post.

## Decisions and Alternatives Rejected

| Decision | Alternatives rejected and why |
|---|---|
| **Re-aim the web pre-filter to `quantityshiprecv`** (chosen) | *Only map the two fields, leave the packed-based filter*: the filter would still admit closed lines and rely entirely on the server to strip them — later, and the pre-filter would keep disagreeing with the guard. |
| **Exclude closed lines in the pre-filter** (chosen) | *Let the server strip handle it*: wasted payload, and the 400 risk remains for any path that calls the service without the guard. |
| **Also map `IsClosed` / `QuantityAlreadyShipped`** (chosen) | *Pre-filter change only*: fixes fully-shipped closes, but a **manually** closed line with remaining quantity > 0 still passes the remaining filter and is only removed by the server guard — which needs the flag. Both are required. |
| **Resolve the item-named variant by name** (chosen) | *Keep only the line-index regex*: the reported phrasing emits no index, so the log stays blind. *Match `for (.+?)\.`*: truncates a dotted name at `1.5`. |
| **Leave `LineQuantityBackOrdered` / `LineQuantityPacked` in the DTO** (chosen) | *Remove them*: still part of the payload contract and the UI; only the remaining formula needed to change. |

## Residual Risk

1. **Medium — the sibling web VRA/Returns packing path has the same gap and is untouched.** `VendorReturnAuthorizationItemReceiptPackingHandler.cs:49` uses the identical packed-based pre-filter, and its `PostVendorReturnAuthorizationDTO` has **no** `IsClosed` / `QuantityAlreadyShipped` fields at all, so its guard cannot be armed without a DTO change. It routes to `SaveVRAItemFulfillment`, so it does not produce this exact toast. Tracked as separate work; not fixed here.
2. **Low — client-reported verification; not reproduced in this session.** The client confirmed on 2026-10-19 that the original failing TO now saves, but this session could not reproduce `totalLines=561` or destructure the posted payload. The code gaps are objective; the behaviour matrix remains derived, not observed.
3. **Low — the web handler itself has no unit test.** There is no `Web.BlazorServer` test project; the handler change is covered only indirectly (server guard tests + the new decoder tests). A handler test with a fake `ISender` would lock the payload assertion.
4. **Low — a pre-existing stock-vs-display unit assumption is now live on web.** `FindScansExceedingRemaining` compares `ScannedQuantity` (display UoM) against `RemainingQuantity` (stock UoM). It was inert on web before (remaining was `LineQuantity − 0`); now it can fire. The mobile path has shared this assumption since 2026-10-16 without a reported false rejection, so it is expected benign, but a TO with `UoMRate < 1` is unverified.
5. **Low — the item-name decoder depends on `MaterialName` formatting.** A localized or trimmed name would fail to match; the fallback reports the raw name rather than dropping it.

## Follow-up Actions

| # | Action | Status |
|---|---|---|
| 1 | Web pre-filter → `LineQuantity − QuantityAlreadyShipped`; exclude closed lines | **Done** |
| 2 | Map `IsClosed` / `QuantityAlreadyShipped` onto the web DTO | **Done** |
| 3 | `DecodeTOItemFulfillmentLine` + wire both TO catch blocks | **Done** |
| 4 | Decoder regression suite (5 cases) | **Done** |
| 5 | `graphify update .` | **Done** |
| 6 | Verify the original failing TO saves; destructure the payload | **Done — client-confirmed 2026-10-19** |
| 7 | Verify an open and a partially fulfilled TO are unchanged | **Open — client** |
| 8 | File and fix the VRA/Returns web packing gap (DTO + handler) | Open — recommended |
| 9 | Add a `Web.BlazorServer` handler test (fake `ISender`) | Open — recommended |

## Validation Limits

- **Live verification is client-reported** (2026-10-19); this session could not reproduce the failing order or destructure the payload. The behaviour matrix is derived from source reading.
- **The failing order's fulfilment state was inferred** from the reported `scannedLines=3 totalLines=561`, not observed.
- **The web handler change is not unit-tested** (no test project); the server guard and decoder are.
- **Scope is the web STR Item Receipt path only.** The VRA/Returns web packing path remains unguarded (residual risk 1).
