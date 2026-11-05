# STR Returns — Partial Save Rejected with `[DUPLICATE_KEYS]`

> Status: implemented; not yet exercised against the NetSuite sandbox.
> Date: 2026-10-20.
> Scope: mobile STR Returns (`TOxReturnxItemFulfillmentItemView`), the shared returns Item
> Receipt service (`SaveReturnsItemReceipt`), and the load-time normalization on STR
> Receiving (`TOxItemFulfillmentItemView`).
> Related: `.opencode/docs/receiving-duplicate-orderline-fix.md` (same defect family, TO
> receiving); `.opencode/specs/2026-10-13-returns-receipt-open-quantity-divergence/`
> (`quantityshiprecv` ledger).

---

## 1. The reported symptom

```
Action failed: Error while posting BAD Returns Item Receipt:
NetSuite 400 Bad Request: Bad Request(400):
[DUPLICATE_KEYS] There are multiple sublist lines with the same identifier.
Make sure every line is unique
```

Reproduction reported by the operator:

```
Scan -> Save -> "Missing Items?" confirmation -> Cancel
     -> Remarks -> Save with remarks -> error
```

A second, independent symptom was also reported: the **"Over-scanning item"** toast appeared
before anything had been scanned for the line.

## 2. Root causes

Two defects produced the `DUPLICATE_KEYS` error. Either one is sufficient.

### 2.1 Client: the save rewrote the list it was reading (returns screen only)

`SaveScan` rebuilt `ReturnsItems` from its own output and then appended a stale
`MissingReturnsItems` field:

```csharp
ReturnsItems = ReturnsItems.Where(x => x.NSLineQuantityReceived != 0)
    .Select(x => new TOxItemFulfillmentLineVM { /* ... */ })   // IsMissing NOT copied
    .Concat(MissingReturnsItems)                               // never reset
    .ToList();
```

`IsMissing` was dropped by the projection, so every previously-tagged missing row came back
as a normal row, and `MissingReturnsItems` was concatenated again. On the second save attempt
the same `LineSequenceNumber` appeared twice in the good/bad bucket, which is exactly what
`item[].orderline` must not do.

The step that *looked* like it caused the failure — cancelling "Missing Items?" — was not
involved: the `_dialogService.Confirm(...)` result was assigned to `confirm` and never read.
Missing rows were always built.

### 2.2 Query fan-out: one transaction line becomes several rows (both screens)

`Integration.NS/NSScripts/NS_TO_Get_Itemfulfillment_Items.sql` joins the item's preferred bin
(`ibq`) and the vendor's bin assignment (`ivb`) — both one-to-many — and emits
`LineSequenceNumber = oto.id` per joined row. `ReturnsIRPayloadDTO.CreateForItemReceipt` maps
rows 1:1 to `item[].orderline`, so a line with two matching bins posted the same `orderLine`
twice.

The TO receiving screen has had the same exposure since
`receiving-duplicate-orderline-fix.md`; its restlet path is protected by
`DeduplicateReceivingLines` but its **load** still received the fan-out rows.

### 2.3 False over-scan toast

```csharp
var isOverScan = line.ScannedQuantity >= line.NSLineQuantityReceived;
```

For a line with no remaining quantity (`0 >= 0`) this fired before the first scan. The check
was also redundant: the following `scanQty > remainingQty` comparison already rejects a scan
that does not fit.

### 2.4 Latent: missing rows lost their transfer category

The missing-row clone copied every field except `TransferCategory`. With only missing rows in
the request, the server derived `receivingCategory = 2` (BAD) instead of the document's
category. Unreachable through the UI (the Save button is disabled at zero scans) but wrong.

## 3. Business rules applied

Confirmed with the receiving owner before implementation.

| Question | Decision |
|---|---|
| "Missing Items?" → **Proceed** | Post scanned lines **and** tag the remainder as missing (category 5), mirroring STR Receiving. |
| "Missing Items?" → **Cancel** | Post **only** the scanned lines; the remainder stays open for a later save. |
| Are remarks still mandatory? | Yes — unchanged. Both screens already enforce it (`TOxItemFulfillmentItemView.razor.cs:755`, `TOxReturnxItemFulfillmentItemView.razor.cs:656`). |
| Two Item Receipt documents per save (bucket + missing bucket) | Accepted for now. |
| Duplicate `orderLine` handling | Collapse on the client **and** on the server; fail fast when copies disagree on quantity. |
| Cross-check scanned + missing against remaining | Added server-side. |
| SQL `SELECT DISTINCT` | Deferred to a separate change. |
| Scope | Both screens (STR Returns and STR Receiving). |

## 4. Fix applied

### 4.1 STR Returns — `TOxReturnxItemFulfillmentItemView.razor.cs`

| Location | Change |
|---|---|
| `:42` | `MissingReturnsItems` field replaced by `SavePayload`, the immutable snapshot posted by the action. |
| `:91-95` | Load collapses fan-out with `GroupBy(LineSequenceNumber)`, preferring the copy with a preferred bin (the receipt builder resolves the bin from the row). |
| `:173` | `ActionSaveScan` posts `PostReturn = SavePayload` instead of the live scan list. |
| `:513-585` | Missing rows are built into a **local** list, and only when `confirm is true`. `IsMissing` and `TransferCategory` are now carried over. |
| `:593-634` | `SavePayload` is projected from `ReturnsItems` (which is no longer reassigned), concatenated with the local missing list, then `GroupBy((LineSequenceNumber, IsMissing))` guarantees one entry per key per bucket. |
| `:636` | "Nothing to save" warning now checks `SavePayload`. |
| `:374-396` | Over-scan guard replaced: `remainingQty <= 0` reports the real condition ("no remaining quantity to receive"), and the existing `scanQty > remainingQty` check handles partial overscan. |

Because `ReturnsItems` is no longer rewritten, re-running the save rebuilds the same payload
from the same scan state — a retry is idempotent instead of additive.

### 4.2 STR Receiving — `TOxItemFulfillmentItemView.razor.cs`

Load-time normalization only (`:97-104` for `GoodIFItems`, `:143-150` for `BadIFItems`).

Its save path was already correct and was **not** touched: `:603` resets `MissingIFItems`,
`:618` honours the confirm, and `IFItems` is rebuilt at `:683` from `GoodIFItems` /
`BadIFItems`, never from its own output.

### 4.3 Service — `Integration.NS/Services/NetSuiteApiClientService.cs`

| Member | Purpose |
|---|---|
| `CollapseReturnLines` (`:1249`) | One row per `LineSequenceNumber` **per bucket**. Prefers a scanned row, then one carrying a bin. Zero-quantity fillers never count as a quantity disagreement; two non-zero quantities that differ throw `InvalidOperationException` naming the line and both values. |
| `ReturnQuantityTolerance` (`:1288`) | `0.00001m`, matching `FormatHelper.RoundOfNearestHundredThousands` (`Math.Round(value, 5)`). Prevents the UoM division from rejecting legitimate saves. |
| `FindReturnQuantityOverflows` (`:1300`) | For each line, `scanned + missing <= (LineQuantity - (LineQuantityReceived + LineQuantityBackOrdered)) / UoMRate`. A non-positive `UoMRate` is reported rather than guessed. |
| `SaveReturnsItemReceipt` (`:1341`) | Collapses both buckets, runs the quantity check before the first POST, fixes the bucket labels (the good bucket used to say "BAD", the missing bucket "Missing TO"), and logs the failing payload — the PO/TO paths already did. |

### 4.4 Tests

`Integration.NS.Tests/Services/NetSuiteApiClientServiceReturnsDedupeTests.cs` — 10 tests:
fan-out collapse, bin-carrying copy wins, diverging quantities throw, good+missing pair
survives in separate buckets, collapsed payload has unique `orderLine`, quantity check
(within/beyond remaining, through `UoMRate`, missing rate), and a fully scanned line with a
missing row is rejected.

## 5. Behaviour after the fix

Line with 3 open units, operator scans 1:

| Action | Documents posted |
|---|---|
| Save → Proceed ("Missing Items?") | IR #1: 1 unit, status 1. IR #2: 2 units, status 6. |
| Save → Cancel | IR #1: 1 unit, status 1. 2 units remain open on the document. |
| Save again with nothing new scanned | Same payload as the previous attempt; no duplicate `orderLine`. |
| Scan the 2 remaining units, then save | IR #1: 2 units, status 1. No missing document. |

## 6. What this does not fix

- **SQL fan-out at the source.** `NS_TO_Get_Itemfulfillment_Items.sql` still emits duplicate
  rows; both clients and now the service collapse them. A `SELECT DISTINCT` / `GROUP BY`
  there is the durable fix and should be its own change.
- **Two documents are not atomic.** If the bucket POST succeeds and the missing POST fails,
  the client state is stale. The error toast still says so; the operator should refresh
  before retrying. A merged single document was deferred pending a sandbox check of whether a
  status-6 assignment is accepted under category 1.
- **Stale devices.** The quantity check compares against the figures carried in the request.
  It catches an inconsistent or buggy client, not device A saving first and device B posting
  stale numbers. Closing that needs a pre-POST re-query of the transfer order — the web
  `PostItemReceipt` path already does this; mobile does not.
- **Missing bucket without a bin.** The missing-row clone (both screens) does not copy
  vendor/preferred bin ids, so at a bin-tracked location the payload's `binNumber` is null.
  This matches the working STR Receiving shape and was left unchanged; watch for a
  "configure the inventory detail" rejection on the missing document during verification.
- **Dead route `/receiving/returns/{OrderNumber}`** (`ReturnsItemView`) still posts to
  `transferOrder/0`. Nothing navigates to it; quarantine separately.
- **Duplicate confirm / duplicate error toast** (`ActionFactoryService`) — UX only, unchanged.

## 7. Verification

Automated:

```
dotnet build Integration.NS/Integration.NS.csproj
dotnet build Mobile.MAUI/Mobile.MAUI.csproj
dotnet test  Integration.NS.Tests/Integration.NS.Tests.csproj
```

`GridResultCacheTests.Key_index_does_not_outlive_the_cache_it_tracks` is timing-flaky in the
full run (passes in isolation); it is unrelated to this change.

Manual (sandbox), on a document with one line of 3 units:

1. Scan 1 → Save → **Proceed** → remarks → expect IR (1, status 1) and IR (2, status 6), no 400.
2. Scan 1 → Save → **Cancel** → remarks → expect IR (1, status 1) only; the line still shows 2 open.
3. Repeat step 1 twice without new scans → second attempt must not produce a duplicate error.
4. Scan 1 of 2 and save; then scan the last unit and save → no missing document.
5. `POST /api/Receiving/Returns/ItemFulfillment/Items` → no repeated `lineSequenceNumber`.
6. Same checks on STR Receiving (the screen that was used as the reference).

Server log on failure now includes the posted payload:

```
{Bucket} Returns Item Receipt failed. Url: ... Payload: {...}
```

## 8. Files changed

| File | Change |
|---|---|
| `Mobile.MAUI/Components/Pages/Receiving/Returns/TOxReturnxItemFulfillmentItemView.razor.cs` | Payload snapshot, honoured confirm, load normalization, over-scan guard. |
| `Mobile.MAUI/Components/Pages/Receiving/TransferOrder/TOxItemFulfillmentItemView.razor.cs` | Load normalization for good/bad lists. |
| `Integration.NS/Services/NetSuiteApiClientService.cs` | `CollapseReturnLines`, `FindReturnQuantityOverflows`, labels, payload logging. |
| `Integration.NS.Tests/Services/NetSuiteApiClientServiceReturnsDedupeTests.cs` | New: 10 tests. |
