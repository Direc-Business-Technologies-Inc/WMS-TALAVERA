# Fix: PO Item Receipt line fan-out, missing fillers, and a blind diagnostic

Date: 2026-10-19
Status: Fix in source, builds clean (`Integration.NS`, `Mobile.MAUI`), 53/53 targeted tests green — **awaiting client verification**
Mode: Bugfix / RCA / Follow-up to [2026-10-17 PO closed-line guard](2026-10-17-po-item-receipt-closed-line-guard-fix.md) and [2026-10-17 PO `?replace=` fix](2026-10-17-po-item-receipt-missing-inventory-assignment-replace.md)
Scope: Receiving module — GOOD/BAD PO Item Receipt (`SavePOItemReceipt` → `purchaseOrder/{id}/!transform/itemReceipt`), the PO line query `NS_PurchaseOrder_Get_Items.sql`, and the mobile PO line selection (`PurchaseOrderItemView.SaveScan`)
Related:
- [2026-10-15 `?replace=` / `INVALID_PARAMETER` fix](2026-10-15-if-replace-param-invalid-parameter-fix.md) — Root Cause 2 (duplicate `orderLine` from query fan-out) and the cross-check that the itemReceipt transform **tolerates** mixed filler coverage.
- `.opencode/specs/2026-10-08-mobile-po-item-receipt-line-selection` — Req 2.1: the mobile must send every open line so the transform's static sublist is satisfied. Unmet by the client until this change.
- `NS_TO_Get_Itemfulfillment_Items.sql`, `NS_TO_x_Packing_Get_Items.sql` — the proven GROUP BY + `MIN` fan-out shape this fix copies.

Changed files:
- `Integration.NS/NSScripts/NS_PurchaseOrder_Get_Items.sql` — grouped `ibq` (preferred bin) and grouped `ivb` (vendor bin assignment) subqueries
- `Integration.NS/Services/NetSuiteApiClientService.cs` — `BuildStamp`, `DecodeItemReceiptLine`, missing-bin pre-flight guard; both PO catch blocks now emit `build=…` + the decoded line
- `Application.DataTransferObjects/Transactions/Receiving/NS/Payload/PurchaseOrderIRPayloadDTO.cs` — the `GroupBy` now prefers the scanned row
- `Mobile.MAUI/Components/Pages/Receiving/PurchaseOrder/PurchaseOrderItemView.razor.cs` — `SaveScan` posts every open line
- `Integration.NS.Tests/Services/NetSuiteApiClientServicePOReceiptDiagnosticsTests.cs` (new)
- `Integration.NS.Tests/Payloads/PurchaseOrderIRPayloadDTOTests.cs` — one added regression test

## Summary

Receiving an open purchase order at a bin-tracked location fails at the last step, on the **scanned** line:

```
Action failed: Error while posting GOOD PO Item Receipt. NetSuite 400 Bad Request:
Bad Request(400): [USER_ERROR] Error while accessing a resource.
Please configure the inventory detail in line 1 of the item list.
[diag replace=True usedBin=True scannedLines=1 totalLines=1]
```

and again with `[diag replace=True usedBin=True scannedLines=20 totalLines=20]`.

The `?replace=` parameter is present (`replace=True`), so this is **not** the sibling defect. The counters are consistent with a client that submitted only the lines it had scanned, but they do not prove it: an order whose open lines were all scanned reports equal counts on either build. The reports therefore cannot distinguish a stale client from a fully-scanned order.

This is the same NetSuite text as the two earlier PO specs but a third route to it. All three are now closed:

| Route to "configure the inventory detail in line N" | Closed by |
|---|---|
| Body's `inventoryDetail` ignored (no `?replace=`) | 2026-10-17 `?replace=` fix |
| Addressing a fully received line absent from the sublist | 2026-10-17 closed-line guard |
| **A fanned-out / mis-binned line and a partial filler set** | **this document** |

## Confirmed Business Decisions

Confirmed with the client on 2026-10-19 before finalising the fix:

- **Vendor bin source — the item's preferred vendor, not the PO vendor.** A PO line resolves `VendorBinAssignmentId` from the item's preferred vendor (`itemvendor.preferredvendor = 'T'`) regardless of which vendor the PO was raised against. The SQL therefore stays scoped by item, subsidiary and location; it deliberately does **not** join on `t.entity`.
- **Bin precedence — vendor bin, then preferred bin.** Per item: if a vendor bin assignment exists at the location, the line is received there; otherwise it falls back to the item's preferred bin. This is the payload's existing `VendorBinAssignmentId != 0 ? vendor : preferred` rule, so no payload change was needed.
- **The bin requirement is per line and per quality.** A PO can carry lines at different locations, so the guard judges each line on its own `IsLocationUsedBin` (not an order-level flag). It also judges each good/bad bucket separately, mirroring the payload builder.

## Root Cause

Three defects cooperate on the same transform, plus the diagnostic could not name the offender.

### 1. The PO line query fans a line out and can resolve the wrong vendor bin

`NS_PurchaseOrder_Get_Items.sql` joined the vendor bin-assignment record on the **vendor alone**:

```sql
LEFT JOIN customrecord_dbti_vendor_bin_assignment ba ON t.entity = ba.custrecord_dbti_vba_vendor
```

There is no item, subsidiary, or location predicate, and no grouping. A vendor with N bin-assignment records (across items and locations) therefore multiplies every PO line into N rows — and `ba.custrecord_dbti_vba_assigned_bin AS VendorBinAssignmentId` can resolve to a bin that belongs to a **different item or location**. The preferred-bin subquery was ungrouped too, so an item with more than one preferred bin at the location fanned out as well.

Consequences, both fatal at a bin-tracked location:

- a **duplicate `orderLine`** in the payload sublist (NetSuite rejects duplicate entries), and
- a **wrong or empty `binNumber`** — the exact condition NetSuite reports as "configure the inventory detail".

The transfer-order queries had already been fixed against this fan-out (`NS_TO_Get_Itemfulfillment_Items.sql`, `NS_TO_x_Packing_Get_Items.sql` both GROUP BY and `MIN` the same two subqueries). The PO query never received the fix.

### 2. The mobile posted only its scanned lines

`PurchaseOrderItemView.SaveScan` built its post list with:

```csharp
POItems = GoodPOItems
    .Where(g => g.ScannedQuantity > 0 && g.ScannedQuantity + badQty <= g.NSLineQuantityReceived)
    .Concat(BadPOItems.Where(x => x.ScannedQuantity > 0))
    ...
```

Only scanned lines are sent, so the payload's `item.items` is a **partial subset** of the transform's open-line sublist. Per Req 2.1 of `.opencode/specs/2026-10-08-mobile-po-item-receipt-line-selection`, every open line must be sent — the unscanned ones as detail-free fillers — matching the production Returns receipt (`ReturnsIRPayloadDTO`), which is built that way and works.

### 3. No pre-flight bin guard on the PO path

The TO path runs `RequiresBin` before posting and fails with an operator-actionable message naming the location and items. The PO path ran none, so a line with no resolvable bin reached NetSuite and came back as the cryptic 400.

### 4. The PO diagnostic was blind

`SavePOItemReceipt` emitted only counts:

```csharp
var diag = $"[diag replace={url.Contains("replace=")} usedBin={isUsedBin} scannedLines={…} totalLines={Data.Count}]";
```

The itemFulfillment decoder, by contrast, already turned "line N of the item list" into the offending DTO row. The PO path had no equivalent, so a field failure could not name the line without a server-logs round trip.

## The Fix

Ordered as implemented; the SQL fix is the root-cause fix and the rest is defence in depth plus observability.

### 2.3 — Root cause: de-fan the query (mirrors the TO queries)

Preferred bin — group by item + location and take `MIN(bin)`, exactly as the TO scripts do:

```sql
LEFT JOIN (
    SELECT ibq.item, MIN(ibq.bin) AS bin, b.location
    FROM itembinquantity ibq
    INNER JOIN bin b ON b.id = ibq.bin
    WHERE ibq.preferredbin = 'T'
    GROUP BY ibq.item, b.location
) ibq ON ibq.item = i.id AND ibq.location = tl.location
```

Vendor bin assignment — join through `itemvendor` (preferred vendor) and scope by item, subsidiary **and** location, collapsing with `MIN`:

```sql
LEFT JOIN (
    SELECT iv.item, iv.subsidiary,
           MIN(ba.custrecord_dbti_vba_assigned_bin) AS custrecord_dbti_vba_assigned_bin,
           ba.custrecord_dbti_vba_location
    FROM itemvendor iv
    JOIN customrecord_dbti_vendor_bin_assignment ba ON iv.vendor = ba.custrecord_dbti_vba_vendor
    WHERE iv.preferredvendor = 'T'
    GROUP BY iv.item, iv.subsidiary, ba.custrecord_dbti_vba_location
) ivb ON ivb.item = i.id
    AND ivb.subsidiary = t.subsidiary
    AND ivb.custrecord_dbti_vba_location = tl.location
```

The projection alias is preserved (`ivb.custrecord_dbti_vba_assigned_bin AS VendorBinAssignmentId`), so no mapping changes.

### 2.2 — Payload: prefer the scanned row when a line still repeats

The query fix removes the fan-out, but a stale or alternative client can still send a repeated line, so the payload `GroupBy` no longer trusts row order. It prefers the scanned row — the same tie-break `DeduplicateFulfillmentLines` applies — and only falls back to the whole group when the quality bucket is empty:

```csharp
var postBucket = isGoodPost ? group.Where(x => !x.IsBad) : group.Where(x => x.IsBad);

var line = postBucket.OrderByDescending(x => x.ScannedQuantity > 0)
                     .ThenByDescending(x => x.ScannedQuantity)
                     .FirstOrDefault()
          ?? group.OrderByDescending(x => x.ScannedQuantity > 0)
                  .ThenByDescending(x => x.ScannedQuantity)
                  .First();
```

The good/bad split behaviour is unchanged: for a line scanned 8 good / 2 bad, the good post takes the good row and the bad post the bad row.

### 2.1 — Mobile: post every open line

```csharp
POItems = GoodPOItems
    .Where(g => g.NSLineQuantityReceived != 0)
    .Concat(BadPOItems.Where(x => x.ScannedQuantity > 0 && x.NSLineQuantityReceived != 0))
    ...
```

`GoodPOItems` already holds every open line; the unscanned ones travel as detail-free fillers (`isReceived: false`), exactly as the Returns receipt does. Over-scans are deliberately left to the server's remaining-quantity guard, which names the line. The "no scanned items to save" early return is unchanged.

**Deliberately not ported from TO:** the TO filler carries an empty `inventoryAssignment` (`TransferOrderIFPayloadDTO`). The itemReceipt transform tolerates a detail-free filler — proven by the production Returns receipt and recorded at the 2026-10-15 spec's cross-check — so the PO filler stays detail-free and no shape is invented.

### 2.5 — Pre-flight missing-bin guard

Runs after the closed-line strip and the over-scan check, before either post. It judges each line on its **own** location flag and each good/bad bucket separately, mirroring the payload builder:

```csharp
internal static List<string> FindPOMissingBinLines(List<PostPurchaseOrderDTO> lines) =>
    lines
        .Where(x => x.ScannedQuantity > 0)
        .GroupBy(x => (x.LineSequenceNumber, x.IsBad))
        .Select(g => g.OrderByDescending(x => x.ScannedQuantity > 0)
                      .ThenByDescending(x => x.ScannedQuantity).First())
        .Where(x => RequiresBin(x.IsLocationUsedBin, x.ScannedQuantity, x.VendorBinAssignmentId, x.NetsuiteMaterialPrefferedBinId))
        .Select(x => $"WMS line {x.LineSequenceNumber} ({x.MaterialCode} - {x.MaterialName}) at {x.LocationName}")
        .ToList();
```

`SavePOItemReceipt` throws before any NetSuite call when the list is non-empty.

Two deliberate choices follow from the confirmed decisions:

- **Per-line location flag.** `RequiresBin` receives `x.IsLocationUsedBin`, not an order-level `Data.Any(...)`. A PO can mix a bin-tracked line and a non-bin line; the order-level flag would falsely reject the non-bin line of a mixed PO.
- **Per-quality buckets.** Grouping is `(LineSequenceNumber, IsBad)` because the good and bad posts are separate NetSuite calls that each pick their own row. Grouping by line alone checks only the higher-quantity row and can let a bin-less bad scan through to the cryptic 400.

### 0 & 1 — Diagnostics

`BuildStamp` (assembly informational version) is appended to both PO diagnostics, and `DecodeItemReceiptLine` decodes "line N of the item list" into the offending WMS line, naming `usedBin`/`vendorBin`/`preferredBin`/`qty`. The decoder indexes the **same sublist the builder emits** — grouped by `LineSequenceNumber` in first-appearance order, so fillers occupy positions and fanned-out lines collapse to one — which is why the caller passes the full `Data`, not the good/bad bucket.

The counters fingerprint the mobile build **only in one direction**: a payload that carries unscanned fillers reports `totalLines > scannedLines`. Equal counts are inconclusive — an order whose open lines were all scanned reports them equal on either build — so do not treat equality as proof of a stale client. A dedicated app-version header remains the reliable signal (follow-up #12).

## Behaviour Matrix (current code)

| Query / client state | Result |
|---|---|
| Item with several preferred bins at the location | **one** row per line (`MIN(bin)`) |
| Vendor with several bin assignments | **one** row per line, scoped to the item's preferred vendor + subsidiary + location |
| Mobile sends only scanned lines (old APK) | fillers missing; the transform may still reject — deploy the new APK |
| Mobile sends every open line (new APK) | unscanned open lines travel as detail-free fillers |
| Line repeats after the query fix (stale client) | `GroupBy` keeps one entry, preferring the scanned row |
| Bin-tracked location, scanned line, no bin resolved | **blocked before any NetSuite call**, naming WMS line + material + location |
| Non-bin location | guard never fires (`RequiresBin` gated on the line's own `IsLocationUsedBin`) |
| Mixed-location PO (bin + non-bin lines) | guard judges each line's own flag; non-bin lines are never flagged |
| Line scanned good and bad, one row has a bin | each quality bucket is checked; the bin-less scanned row is blocked before posting |
| NetSuite still rejects | diagnostic names the build and the decoded line |

## Tests

**New:** `Integration.NS.Tests/Services/NetSuiteApiClientServicePOReceiptDiagnosticsTests.cs` — 13 test methods (16 cases once the theory data is expanded):
- decoder returns empty for a message without the marker, and for an out-of-range index
- index maps to the open line at that payload position, including unscanned fillers
- the good post decodes the good row and the bad post the bad row for a split line
- a fanned-out duplicate collapses to a single position (the second index returns empty) and the decoded row is the scanned one with its bin
- the decoded string reports `usedBin` / `vendorBin` / `preferredBin`
- `RequiresBin` flags a bin-tracked scanned line with no bin and ignores the three non-flagging cases
- `FindPOMissingBinLines` does not flag a non-bin line on a **mixed-location** PO
- a **good/bad split line** is checked per bucket: the bin-less good row and the bin-less bad row are each flagged even when the other quality has a bin
- a **fanned-out** line is judged on its scanned row, not on its unscanned duplicate

**Added:** `PurchaseOrderIRPayloadDTOTests.Fanned_out_line_receives_the_scanned_duplicate_and_its_bin` — the unscanned duplicate is listed **first**, so the old `FirstOrDefault` would have dropped the receipt; the payload must receive the scanned row and bin `55`.

**Existing and still green:** `PurchaseOrderIRPayloadDTOTests` (incl. every-filler and split-line cases), `NetSuiteApiClientServicePOClosedLineGuardTests`, `NetSuiteApiClientServiceBinRequirementTests`.

Builds: `dotnet build Integration.NS` → 0 errors; `dotnet build Mobile.MAUI -f net9.0-android` → 0 errors (pre-existing warnings only).
Tests: filtered run of the four PO suites → **53/53 passed**.

## Verification Plan

Must be confirmed on the client; no NetSuite sandbox was available in this session.

1. **An ordinary open PO at a bin-tracked location** — must post, and the receipt must land in the resolved bin (this exercises the changed SQL: confirm the bin is the intended one, not merely accepted).
2. **An item with several preferred bins / a vendor with several bin assignments** — must show **one** line per PO line (no duplicates) and post once.
3. **A partially received PO** — scan ≤ remaining posts; scan > remaining fails with the WMS-line message.
4. **A line with no resolvable bin** — must fail with the new guard message, not a raw NetSuite 400.
5. **Any remaining failure** — the toast must carry `build=…` and a decoded `lineN=WMSLine …` segment.

### Deployment notes

- The **SQL** and the **server** changes deploy by redeploying the API; they do not need the APK. The **mobile** change (Req 2.1) needs an APK rollout.
- The diag counters give a one-way signal: after rollout a partially-scanned order should report `totalLines > scannedLines`. Equality is inconclusive (a fully-scanned order reports equal on either build), so do not use it alone to confirm the new APK.

## Decisions and Alternatives Rejected

| Decision | Alternatives rejected and why |
|---|---|
| **Fix the query fan-out (GROUP BY + `MIN`)** (chosen) | *Only dedupe in the payload*: leaves a wrong `binNumber` selected for a fanned line; the transform would still reject. The query is the root cause. |
| **Resolve the vendor bin from the item's preferred vendor** (confirmed) | *Scope the assignment by `t.entity` (the PO vendor)*: rejected by the client — the assignment belongs to the item's preferred vendor. The scope still includes item, subsidiary and location, which removes the fan-out and the foreign-bin selection. |
| **Prefer the scanned row in the payload** (chosen) | *Depend on query row order*: order is not guaranteed; an unscanned duplicate could win and silently drop a receipt. |
| **Mobile posts every open line** (chosen) | *Post only scanned lines and rely on `?replace=`*: Req 2.1 and the Returns precedent both require the full sublist. |
| **Leave PO fillers detail-free** (chosen) | *Port the TO empty-assignment filler*: the itemReceipt transform tolerates a detail-free filler (Returns IR proves it); inventing a shape is risk without benefit. |
| **Guard on the line's own `IsLocationUsedBin`, grouped by (line, quality)** (chosen) | *Header-level `isUsedBin`*: false-rejects the non-bin line of a mixed-location PO. *Per-line without the quality split*: checks only one row of a good/bad split and can still emit the cryptic 400. |
| **`build=…` + `scannedLines/totalLines` for build tracking** (chosen) | *Add a mobile version header now*: the counters distinguish old from new **only while the order has unscanned open lines**; a fully-scanned order reports equal counts either way, so a header remains a stronger signal for a later change. |

## Residual Risk

1. **Medium — the vendor bin now comes from the item's preferred vendor by design (confirmed 2026-10-19).** Saves that previously resolved `VendorBinAssignmentId` from the PO vendor's assignment will now use the preferred vendor's assignment, fall back to the preferred bin, or resolve none. Confirm on a live open PO that the resolved bin is the intended one. Also verify that `customrecord_dbti_vendor_bin_assignment` records carry `custrecord_dbti_vba_location`; a NULL location no longer matches and silently drops the vendor bin.
2. **Medium — the filler-coverage assumption is inherited, not re-proven on PO.** Req 2.1 and the Returns precedent say the full sublist is required; the PO transform is not instrumented to prove it live.
3. **Low–medium — the mobile needs an APK.** Until it ships, the fillers are still missing; the server can suppress over-scans and name offenders but cannot manufacture the unscanned lines the transform expects.
4. **Low — the decoder mirrors the builder.** If the payload's `GroupBy` is ever changed without the decoder, the decoded index drifts. Both carry a comment to that effect.
5. **Low — `BuildStamp` is `unknown` if the assembly has no informational version.** Cosmetic; the diag still carries the other fields.
6. **Low — no live verification.** The behaviour matrix is derived from source; the query fix is argued from the TO precedent, not observed.

## Follow-up Actions

| # | Action | Status |
|---|---|---|
| 1 | SQL fan-out fix (`NS_PurchaseOrder_Get_Items.sql`) | **Done** |
| 2 | Payload `GroupBy` scanned-preference | **Done** |
| 3 | Mobile posts every open line | **Done** |
| 4 | PO missing-bin pre-flight guard | **Done** — per line, per quality bucket |
| 5 | `BuildStamp` + `DecodeItemReceiptLine` diagnostics | **Done** |
| 6 | Regression suite + payload test | **Done** — incl. mixed-location and split-quality guard cases |
| 7 | Verify an open bin-tracked PO posts, and in the correct bin | Open — client |
| 8 | Verify an item/vendor with several bins shows one line and posts once | Open — client |
| 9 | Verify the no-bin guard message on a genuinely binned-missing item | Open — client |
| 10 | Verify a mixed-location PO still posts (non-bin lines are not falsely blocked) | Open — client |
| 11 | Ship the mobile APK (Req 2.1) | Open — deployment |
| 12 | Consider surfacing the mobile app version (a header) for explicit build tracking | Open |
| 13 | Remove the temporary `[diag …]` markers once the flow signs off | Open — carried |

## Validation Limits

- **No live NetSuite verification.** Conclusions rest on source reading and the TO precedent; the matrix is derived, not observed.
- **The exact discriminating defect for the two reported POs is not proven.** The reports show only counters; fan-out, a missing bin, or a partial sublist could each produce the message. The change closes all three, so the outcome is expected to be correct even though the single culprit is not isolated.
- **The mobile change is not exercised on a device.** `Mobile.MAUI` compiles for `net9.0-android`; the runtime behaviour of the filler set is unverified.
