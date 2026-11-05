# Fix: PO Item Receipt posts already-received lines (closed-line guard)

Date: 2026-10-17
Status: Fix in source, build clean, 401/401 tests green — **awaiting client verification on the original failing PO (170552)**
Mode: Bugfix / RCA / Follow-up to [2026-10-17 PO Item Receipt `?replace=` fix](2026-10-17-po-item-receipt-missing-inventory-assignment-replace.md)
Scope: Receiving module — GOOD/BAD PO Item Receipt (`SavePOItemReceipt` → `purchaseOrder/{id}/!transform/itemReceipt`)
Related:
- [2026-10-17 PO Item Receipt `?replace=` fix](2026-10-17-po-item-receipt-missing-inventory-assignment-replace.md) — the sibling fix for a *different* PO (`171264`); that document is about the missing `?replace=` parameter, this one about submitting lines that are no longer on the transform. Both surface as the same NetSuite text.
- [2026-10-16 packing bin guard false-positive fix](2026-10-16-packing-bin-guard-false-positive-fix.md) — the pattern this fix borrows: a testable `internal static` predicate plus a pre-flight guard that fails loud with an operator-actionable message.
- `NetSuiteApiClientServiceClosedLineGuardTests` (existing, TO path) — the suite this one mirrors. Its docstring already records the identical failure mode on the `itemFulfillment` transform (ISTR-THM00100000631, 2026-10-16).

Changed files:
- `Integration.NS/Services/NetSuiteApiClientService.cs` — new `IsClosedPOLine`, `PORemainingDisplayQuantity`, `FindPOScansExceedingRemaining`; guard wired into `SavePOItemReceipt`
- `Integration.NS.Tests/Services/NetSuiteApiClientServicePOClosedLineGuardTests.cs` (new, 16 cases)

## Summary

Receiving a **fully received** purchase order fails at the last step:

```
Action failed: Error while posting GOOD PO Item Receipt. NetSuite 400 Bad Request:
Bad Request(400): [USER_ERROR] Error while accessing a resource.
Please configure the inventory detail in line 4 of the item list.
[diag replace=True usedBin=True scannedLines=10 totalLines=10]
```

The failing PO is `170552` (`tranid PO-THM00100000216-TR`), location bin-tracked, all 18 material lines already received. The diagnosis first attached to this error — missing `?replace=` — is **not** the cause here: `[diag replace=True]` proves the parameter was present. On this PO the message is a NetSuite rejection of an insert against a **static sublist line that no longer exists**, because every line the mobile submitted was already fully received.

The `?replace=` fix and this fix therefore address two different ways to reach the same NetSuite text. Both are required: `?replace=` makes a *valid* bin-tracked receipt work; this guard stops an *invalid* receipt from being attempted at all.

## Root Cause

The PO receipt path had **no closed-line guard** — unlike the transfer-order fulfillment path, which has had one since 2026-10-16.

A PO line with no remaining open quantity is absent from the item sublist of the `!transform/itemReceipt` response. Addressing it — with a scanned entry **or** the zero-qty filler the payload builder emits for unscanned lines — is an invalid insert on a static sublist, and NetSuite rejects the entire save.

`SaveTOItemFulfillment` documents and handles exactly this (`NetSuiteApiClientService.cs`):

```csharp
// ... a closed line has no sublist line to address and any orderLine
// reference for it (scanned entry or zero-qty filler) is an invalid insert on a
// static sublist that fails the entire save. Strip closed / fully-shipped lines
// before bucketing so the payload builder only ever receives postable lines.
Data = Data.Where(x => !IsClosedFulfillmentLine(x)).ToList();

var overRemaining = FindScansExceedingRemaining(Data);
if (overRemaining.Count > 0)
{
    throw new Exception($"Cannot post TO Item Fulfillment: scanned quantity exceeds the remaining quantity for: {string.Join("; ", overRemaining)}. Reload the order and rescan the affected line(s).");
}
```

`SavePOItemReceipt` had neither line. It went straight from the received `Data` to `CreateForItemReceipt` to `MakeRequest`.

### Why the flag-based predicate the TO path uses could not be reused verbatim

`IsClosedFulfillmentLine` is `line.IsLineClosed || line.RemainingQuantity <= 0`. The PO predicate cannot lean on the first clause. A SuiteQL check on the failing PO showed every line fully received **while still reporting `isclosed = F`**:

```sql
SELECT tl.linesequencenumber AS line, tl.item AS item_id, BUILTIN.DF(tl.item) AS item_name,
       tl.quantity AS qty, tl.quantityshiprecv AS recvd, tl.isclosed AS closed
FROM transaction t JOIN transactionline tl ON tl.transaction = t.id
WHERE t.tranid = 'PO-THM00100000216-TR' AND tl.mainline = 'F'
ORDER BY tl.linesequencenumber
```

```json
[{"line":1,"item_name":"00000990","qty":24,"recvd":24,"closed":"F"},
 {"line":2,"item_name":"00029255","qty":24,"recvd":24,"closed":"F"},
 {"line":3,"item_name":"00001000","qty":12,"recvd":12,"closed":"F"},
 {"line":4,"item_name":"00000999","qty":36,"recvd":36,"closed":"F"},
 ...
 {"line":18,"item_name":"00053361","qty":240,"recvd":240,"closed":"F"},
 {"line":19,"item_name":"S-PH","qty":-1,"recvd":0,"closed":"F"}]
```

So `isclosed` is **not** a usable signal on purchase orders: a fully received PO line keeps `isclosed = F`. The guard keys on the remaining open quantity instead, which is correct in both states.

`PostPurchaseOrderDTO` carries no `IsClosed` field at all — consistent with the flag being useless here, and the reason no DTO change was needed.

### The three receipts that closed the PO

`nexttransactionlink` (`linktype: ShipRcpt`) shows three item receipts already posted against PO `170552`:

| Receipt `tranid` | Internal id |
|---|---|
| `IR-THM00100000951-TR` | 180498 |
| `IR-THM00100000956-TR` | 180501 |
| `IR-THM00100000959-TR` | 180502 |

The failing post was a **fourth**, redundant attempt against an already-closed order. The `[diag scannedLines=10 …]` marker is consistent: the mobile sent 10 scanned lines, all of them already received.

## How it was found (investigation path)

1. **Confirmed the path and the parameter.** The error string is thrown only in `SavePOItemReceipt`; `[diag replace=True usedBin=True]` rules out the `?replace=` gap that the sibling spec closed for PO `171264`.
2. **Eliminated the bin hypothesis.** SuiteQL showed `preferred_bin_id = 8` and `preferred_bin = WHSE1-BIN-000-ALL` on all 18 lines with `vendor_bin_id` empty, so the payload sent `binNumber: { id: "8" }`. The bin resolved; this is not the "no bin resolved" case.
3. **Eliminated the `orderLine` mapping hypothesis.** `tl.id == tl.linesequencenumber` for material lines 1–18, so `orderLine` addressing was correct.
4. **Found the closed lines.** The SuiteQL above showed `qty == recvd` on **every** material line — the order is 100% received.
5. **Found the duplicate.** `nexttransactionlink` returned the three receipts above.
6. **Found the asymmetry.** The TO path has `IsClosedFulfillmentLine` + `FindScansExceedingRemaining` + the two guard sites; the PO path has nothing.
7. **Found the precedent docstring.** The existing TO guard comment describes this failure mode verbatim, naming a 2026-10-16 field failure — this is the same defect on the second transform.

## The Fix

### 1. A remaining-based predicate (no flag, no DTO change)

`NetSuiteApiClientService.cs`, beside `BuildPOItemReceiptUrl`:

```csharp
internal static bool IsClosedPOLine(PostPurchaseOrderDTO line) =>
    line.LineQuantity - line.LineQuantityReceived <= 0;
```

`LineQuantity` and `LineQuantityReceived` are both in the item's **stock** UoM (`tl.quantity` / `tl.quantityshiprecv`), so the comparison is unit-independent.

### 2. Remaining expressed in the operator's display UoM

`ScannedQuantity` is in the display UoM (the same basis `TransactionVM.NSLineQuantityReceived` uses), so the comparison against a scan has to divide by the rate:

```csharp
internal static decimal PORemainingDisplayQuantity(PostPurchaseOrderDTO line)
{
    var rate = line.UoMRate > 0 ? line.UoMRate : 1m;
    return (line.LineQuantity - line.LineQuantityReceived) / rate;
}
```

### 3. The over-scan descriptor, mirroring `FindScansExceedingRemaining`

Returns one message per offending line, naming the item-list line, material, and the ordered/remaining figures — never for zero-qty fillers.

### 4. The guard, at the top of `SavePOItemReceipt`

```csharp
var scannedButClosed = Data.Where(x => x.ScannedQuantity > 0 && IsClosedPOLine(x)).ToList();
Data = Data.Where(x => !IsClosedPOLine(x)).ToList();

// A stale client that scanned only already-received lines must not be told the post
// succeeded: nothing is left to receive, so fail with the offending lines named.
if (scannedButClosed.Any() && !Data.Any(x => x.ScannedQuantity > 0))
{
    var detail = string.Join("; ", scannedButClosed.Select(x =>
        $"line {x.LineSequenceNumber} ({x.MaterialCode}) - scanned {x.ScannedQuantity}, remaining {PORemainingDisplayQuantity(x)}"));
    throw new Exception($"Cannot post PO Item Receipt for order {orderId}: the scanned line(s) are already fully received ({detail}). Reload the order and rescan the affected line(s); see the receipts already posted against this purchase order in NetSuite.");
}

var overRemaining = FindPOScansExceedingRemaining(Data);
if (overRemaining.Count > 0)
{
    throw new Exception($"Cannot post PO Item Receipt: scanned quantity exceeds the remaining quantity for: {string.Join("; ", overRemaining)}. Reload the order and rescan the affected line(s).");
}
```

Two deliberate properties of the placement:

- **Before `isUsedBin` and `BuildPOItemReceiptUrl`.** `isUsedBin` is `Data.Any(...)`, so it must see the already-stripped set — otherwise a bin-tracked closed line would select the `?replace=` URL for a save that will not happen.
- **Through the unmodified payload builder.** `PurchaseOrderIRPayloadDTO.CreateForItemReceipt` is handed the full (now stripped) `Data` for both buckets, so the closed lines disappear from the scanned entries *and* the zero-qty fillers in one move.

The thrown message reaches the operator unchanged: `PostPurchaseOrderIRCmdHandler` converts any exception into `ApiResult<bool>.ServerError(ex.Message)`, and the mobile shows `result.ErrorMessage` in a toast (`PurchaseOrderItemView.razor.cs`, `ActionSaveScan.OnSuccess`).

## Behaviour Matrix (current code)

| Line state | Scan | Result | Message |
|---|---|---|---|
| Fully received (`qty == recvd`) | any | **stripped from the payload** | — |
| Fully received, and it is the only scanned line | > 0 | **blocked before any NetSuite call** | "…the scanned line(s) are already fully received (line N (ITEM) - scanned X, remaining Y). Reload the order…" |
| Partially received, scan ≤ remaining | > 0 | posted | — |
| Partially received, scan > remaining | > 0 | **blocked before any NetSuite call** | "…scanned quantity exceeds the remaining quantity for: item-list line N (WMS line M) ITEM…" |
| Open | > 0 | posted | — |
| Open, unscanned (filler) | 0 | kept, `isReceived: false` | — |

Precedence: a valid BAD bucket no longer masks an invalid GOOD line, because both guards run over the whole set before either bucket is built.

## Tests

**New:** `Integration.NS.Tests/Services/NetSuiteApiClientServicePOClosedLineGuardTests.cs` — 16 cases across 11 methods:

- the failing repro: a fully received line contributes nothing to the payload line set
- `isclosed = F` regression: a fully received line is closed regardless of the flag
- a fully received line carrying a scan is still excluded
- a partially received line stays open; remaining is `LineQuantity - LineQuantityReceived`
- scan beyond / equal to remaining flagged / allowed; zero-qty filler never flagged
- a valid BAD bucket does not mask an over-remaining GOOD line
- a `[Theory]` boundary sweep (`0/49/50/51/120` against remaining 50)
- a `[Theory]` proving remaining is scaled by `UoMRate` (240 stock units at rate 12 = 20 display units)
- open lines pass through untouched

**Existing and still green:** `NetSuiteApiClientServiceClosedLineGuardTests` (the TO path), `NetSuiteApiClientServicePOReceiptUrlTests`, `PurchaseOrderIRPayloadDTOTests`.

Build: `dotnet build Integration.NS` → **0 errors** (pre-existing warnings only).
Tests: full `Integration.NS.Tests` → **401/401 passed**. New suite alone → **16/16 passed**.

Graph: `graphify update .` → `18555 nodes, 43674 edges, 593 communities` (the SQL-extractor and symbol-less-file warnings are pre-existing and unrelated).

## Verification Plan

Must be confirmed on the client; no NetSuite sandbox was available in this session.

1. **The original failing PO (170552).** A reload followed by rescan must now fail with the new message naming the already-received lines — **not** a raw NetSuite 400. Do **not** attempt to receive PO 170552 again: it is fully received and has three receipts.
2. **A genuinely open PO at a bin-tracked location.** Must post exactly as before — this fix must not touch the working path.
3. **A partially received PO.** Scan ≤ remaining posts; scan > remaining fails with the item-list/WMS-line message.
4. **A mixed save** (one closed line scanned alongside open lines) — confirm the open lines still post and the closed one is dropped, per the matrix.

### Pre-test housekeeping

Confirm the installed mobile build. The PO receiving screen has had its own remaining-quantity gate since 2026-06-05 (`cd385c21`), so it hides zero-remaining lines and blocks over-scans. This fix is **server-side** and deploys independently of the APK.

## Decisions and Alternatives Rejected

| Decision | Alternatives rejected and why |
|---|---|
| **Key on remaining, not `isclosed`** (chosen) | *Reuse `IsClosedFulfillmentLine`'s flag clause*: the SuiteQL above shows `isclosed = F` on fully received PO lines, so the flag would let the failing post through. |
| **Throw when only already-received lines were scanned** (chosen) | *Silently return `true`*: the TO path strips closed lines silently, but here that would tell the operator a post succeeded when nothing was received. *Throw whenever any closed line was scanned*: would block the legitimate case where open lines were also scanned. |
| **Strip closed lines before computing `isUsedBin`** (chosen) | *Guard after the URL is chosen*: `isUsedBin` would be derived from lines that are no longer posted. |
| **`internal static` helpers + unit tests** (chosen) | *Test through the service with a mocked HTTP client*: heavier, and these are pure predicates — the same call made by `RequiresBin` and `DeduplicateFulfillmentLines`. |
| **Leave `NS_PurchaseOrder_Get_Items.sql` alone** (chosen) | *Change `tl.id AS LineSequenceNumber` to `tl.linesequencenumber`*: receipts for open lines have always worked, so the current projection is evidently correct in practice; changing it without a live check risks a new regression. See residual risk 6. |
| **Server-side only** (chosen) | *Also add a mobile revalidation now*: the mobile change cannot ship without an APK rollout, and the server guard is sufficient to stop the 400 and the false success. Tracked as follow-up. |

## Residual Risk

1. **Medium — the mobile still validates against stale in-memory data.** The PO screen loads line quantities once and its post-success handler navigates away (`MarkScansClean()` → `NavigateTo("/receiving")`), re-fetching on re-entry. A screen left open while the PO is received elsewhere (another device, or directly in NetSuite) can therefore submit closed lines. The server guard now turns that into an actionable message instead of a 400, but it does not prevent the wasted attempt. **Follow-up:** revalidate remaining on submit, or refresh before posting.
2. **Medium — a legitimate receipt can now be rejected if the line closed concurrently.** A hard rejection is safer than a partial commit, but the operator must reload. This is the same trade-off the TO path already accepts.
3. **Low–medium — closed lines with a scan are dropped silently when open lines are also present.** Only the "nothing left to post" case raises an error. Matching the TO path keeps the flows consistent, but the operator will not see a per-line notice for a dropped scan.
4. **Low — `isclosed` is ignored entirely.** A line manually closed with remaining quantity > 0 would still be submitted, and the transform would reject it (surfacing via the existing `[diag …]` wrapper). Not observed, but the follow-up below would cover it.
5. **Low — the display-UoM conversion depends on an invariant.** `PORemainingDisplayQuantity` assumes `ScannedQuantity` is display UoM while `LineQuantity`/`LineQuantityReceived` are stock UoM — the same assumption `TransactionVM.NSLineQuantityReceived` makes. Consistent, but unverified on a PO with `UoMRate ≠ 1`.
6. **Low — the `orderLine` projection is fragile.** `NS_PurchaseOrder_Get_Items.sql:14` aliases `tl.id AS LineSequenceNumber`. It coincided with `linesequencenumber` for material lines (1–18) but diverged for the non-inventory `S-PH` line (`id 29` vs `seq 19`). The `S-PH` line is dropped by the location join, so this is latent, not live. Left unchanged deliberately.
7. **Low — no live verification.** All behavioural claims are derived from source plus operator-run SuiteQL; the matrix is not observed.

## Follow-up Actions

| # | Action | Status |
|---|---|---|
| 1 | Remaining-based closed-line predicate + over-scan descriptor + guard in `SavePOItemReceipt` | **Done** |
| 2 | Regression suite `NetSuiteApiClientServicePOClosedLineGuardTests` (16 cases) | **Done** |
| 3 | `graphify update .` | **Done** |
| 4 | Verify the new message on PO 170552 (do not re-receive it) | **Open — client** |
| 5 | Verify an open and a partially received PO still post | **Open — client** |
| 6 | Revalidate remaining on submit in the mobile PO view (needs APK rollout) | Open — recommended |
| 7 | Confirm receipt provenance: `createdby`/`createddate` of IR-…951/956/959 vs the failing post — WMS or NetSuite-originated | Open — data |
| 8 | Reconcile PO 170552 in NetSuite (three receipts; confirm no duplicate inventory) | Open — data |
| 9 | Decide whether to add `isclosed` to the PO line query and close residual risk 4 | Open |
| 10 | Review the `tl.id AS LineSequenceNumber` alias with a live open-PO check | Open |
| 11 | Remove the temporary `[diag …]` markers once the PO receipt flow signs off | Open — carried from the sibling spec |

## Validation Limits

- **No live NetSuite verification.** Conclusions rest on source reading and operator-run SuiteQL; the Behaviour Matrix is derived, not observed.
- **The duplicate-receipt sequence is inferred.** Three receipts exist and the PO is fully received; *how* the operator's screen came to hold closed lines (concurrent receipt vs. long-open screen) is not proven — follow-up 7 would settle it.
- **`IsClosedPOLine` was not exercised against a live transform.** Its correctness rests on the same line-absent-from-sublist behaviour already documented and field-verified on the `itemFulfillment` path.
- **The APK question is closed for this defect.** The TO packing closed-line change (`cb4b0987`) touched only the packing screens and cannot affect PO receiving; the PO screen's own guard predates it (2026-06-05). The only APK-version scenario that could matter is a build older than 2026-06-05, which is worth checking but is unrelated to the packing change.
