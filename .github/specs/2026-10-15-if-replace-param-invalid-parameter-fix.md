# Fix: NetSuite 400 INVALID_PARAMETER on Packing Item Fulfillment (`replace=item.inventoryDetail.inventoryAssignment`)

Date: 2026-10-15
Status: Replace fix VERIFIED in field (one transaction passed); bin guard deployed but refuted; residual `line N` failure traced to SQL fan-out duplicate `orderLine` — dedupe + query fix in source, awaiting client verification
Mode: Bugfix / Follow-up
Scope: Packing module — Item Fulfillment (STR, Returns packing, RTS / Vendor Return Authorization)
Related: [Mobile auth logout + IF sublist RCA](2026-10-14-mobile-auth-logout-and-if-sublist-rca.md); `.opencode/docs/mobile-netsuite-4xx-catalog.md`
Changed files:
- `Integration.NS/Services/NetSuiteApiClientService.cs`
- `Application.DataTransferObjects/Transactions/Packing/NS/Payload/TransferOrderIFPayloadDTO.cs`
- `Application.DataTransferObjects/Transactions/Packing/NS/Payload/ReturnsIFPayloadDTO.cs`
- `Application.DataTransferObjects/Transactions/Packing/NS/Payload/VendorReturnAuthorizationIFPayloadDTO.cs`
- `Integration.NS/NSScripts/NS_TO_x_Packing_Get_Items.sql` (fan-out fix)
- `Integration.NS/NSScripts/NS_TO_x_Return_x_Packing_Get_Items.sql` (fan-out fix)
- `Integration.NS/NSScripts/NS_VendorReturnAuthorization_Get_Items.sql` (fan-out fix)
- `Integration.NS.Tests/Services/NetSuiteApiClientServiceFulfillmentDedupeTests.cs` (new)

## Summary

Packing saves on mobile fail with:

```
Action failed: Error while posting GOOD TO Item Fulfillment. NetSuite 400 Bad Request:
Bad Request(400):
[INVALID_PARAMETER] Resource or sub-resource 'inventoryDetail' from the query
parameter replace was not found in the request body. Provide a valid resource name
in the query parameter or include the resource in the request body.
[diag replace=True usedBin=True scannedLines=1 totalLines=25]
```

The confirmed root cause: on a **bin-tracked location**, the fulfillment transform URL carries `?replace=item.inventoryDetail.inventoryAssignment`, but the payload covered that sub-resource on **only the scanned lines** (1 of 25). The 24 zero-qty filler lines were posted as `{orderLine, itemreceive:false}` with no `inventoryDetail`, and NetSuite's `replace` validation on the **itemFulfillment** transform requires the sub-resource to be present on the body's item entries consistently — it did not find it and rejected the transform.

The 400 survived two earlier fixes because each one addressed a different link in the chain (see Timeline). This document records the full investigation, the three-part fix now in source, and what remains unproven.

## Timeline — why this error kept coming back

| Commit / date | Change to the fulfillment payload / URL | Symptom |
|---|---|---|
| `668cc85e` (original packing build) | **No `inventoryDetail` on any line**, no `?replace=` — NetSuite auto-manages inventory detail | Worked for ordinary items; **400 `Please configure the inventory details in line N`** at bin-tracked locations (the error remembered from web) |
| `54505798` (2026-07-14) | Added flat REST-shaped `inventoryDetail.inventoryAssignment` on scanned lines **+** `?replace=` on the URL | Fixed the bin error; **introduced** the INVALID_PARAMETER whenever the bucket mixes detail and no-detail lines |
| `bb6d5a39` / `1a100a9` (2026-08-14) | Removed `?replace=` from the URL | Back to "invalid sublist" class errors |
| `4b062dcd` (2026-08-26) | Switched detail back to SOAP-shaped `inventoryAssignmentList` wrapper | Wrong shape for REST v1 |
| `85c94246` (2026-10-14, RCA fix) | Restored flat REST shape **+** unconditional `?replace=` | INVALID_PARAMETER — the error under investigation |
| This fix (2026-10-15) | Conditional `replace`, legacy shape for non-bin, **empty assignment on filler lines at used-bin** | Awaiting client verification |

Key insight from the history: the two eras each solve one half of the problem. "Then" (no detail, no replace) worked everywhere except bin-tracked locations. "Now" (detail + replace) works for bin-tracked lines but breaks when the payload mixes lines with and without the sub-resource. Neither commit was wrong on its own.

## Field Progress Log

| Client test | diag | Outcome | Interpretation |
|---|---|---|---|
| Transaction A (used-bin, 1 of 25 lines) | `replace=True usedBin=True scannedLines=1 totalLines=25` | ✅ **PASSED** after the empty-assignment filler fix | The `replace`/INVALID_PARAMETER root cause is **resolved in production** |
| Transaction B (used-bin, 21 of 326 lines) | `replace=True usedBin=True scannedLines=21 totalLines=326` | ❌ `Please configure the inventory detail in line 108 of the item list` | Passed the replace check; now hit catalog case **1a** — a scanned line whose bin resolution came up empty (both `NetsuiteMaterialVendorAssignedBin` and `NetsuiteMaterialPrefferedBinId` are `0`) |
| Transaction B retest (after guard) | same diag, same NetSuite error text | ❌ Identical `line 108` error again | Guard did NOT fire → either the guard build was not deployed, or **all 21 scanned lines have bins resolved** and line 108's cause is something else (prime suspect: **lot/serial-tracked item**; `InventoryAssignmentItem` has no lot/serial fields) |
| Transaction B retest (with line decoder) | `line108=WMSLine 536 00003619 - BARNUTS CHOC PEANUT BAR 20'S vendorBin=0 preferredBin=8 qty=8` | ❌ same error, but the offender row is now **fully decoded** | The decoded row is **well-formed**: SuiteQL confirms bin 8 at OLD WAREHOUSE holds 360 pcs of item 47776 at status 1 (Good), item is not lot/serial (single `inventorybalance` row, no lot fields on the item). Bin data is valid → catalog 1a **refuted**; the cause is the **duplicate `orderLine`** that the same row belongs to (see "Root Cause 2"). Item 00003619 has **12 rows on its Bin Numbers subtab** and was **edited 15:13 the same day**, two hours before the 17:21 failure — consistent with a second bin flagged Preferred at the location. |

### Case 1a (bin-0 scanned line) — analysis and guard

- `line 108` is the **108th entry in the posted `item.items[]` array** (fillers included), not a NetSuite line number — per the 4xx catalog's index rule.
- The failing entry is a **scanned** line: the earlier passing transaction proves filler lines with empty assignments are accepted, so the offender is one of the 21 scanned lines posting `{inventoryStatus, quantity}` with no `binNumber` at a bin-tracked location.
- Second possibility: a **lot/serial-tracked** item — `InventoryAssignmentItem` (`PayloadsDTO.cs:67-77`) has no lot/serial fields at all, so those items cannot satisfy inventory detail regardless of bin data.

**Guard implemented** (`SaveTOItemFulfillment` bad/good buckets, `SaveReturnsItemFulfillment`, `SaveVRAItemFulfillment` bad/good buckets): before posting at a bin-tracked location, any scanned line with both bin IDs `0` now fails fast locally with an operator-actionable message naming each offender:

```
Cannot post GOOD TO Item Fulfillment: location 'OLD WAREHOUSE' is bin-tracked but no bin is resolved for:
item-list line 108 (WMS line 42) WET!DRINKINGWATER - WET! DRINKING WATER 330ML. Assign a bin to these items before saving.
```

The guard's item-list index matches NetSuite's `line N` convention, so the toast and NetSuite's error are directly comparable. Data remedy is outside code: assign the item's bin in the WMS item master (or NetSuite preferred bin) so `NetsuiteMaterialPrefferedBinId` / `NetsuiteMaterialVendorAssignedBin` resolve.

**Follow-up candidate (deferred):** auto-resolve the bin from the transfer order's own inventory detail via SuiteQL when WMS has no bin configured — better UX than blocking, but a bigger change.

### Line-decoder diagnostic (added after the identical retest)

Because the guard did not fire on the retest, the next test must identify the exact DTO row NetSuite is complaining about. All five fulfillment catch blocks now parse NetSuite's `line N of the item list` out of the 400 body and append the decoded row to the diag:

```
| line108=WMSLine 42 WET!DRINKWATER - WET! DRINKING WATER 330ML vendorBin=0 preferredBin=1234 qty=200-3
```

Reading the next result:

| Result at `line108=` | Verdict |
|---|---|
| No `line108=` suffix at all | Guard/diag build not deployed — deployment problem, not code |
| `vendorBin=0 preferredBin=0` | Bin data missing — but guard should have caught it; if both appear, guard never ran (deployment) |
| Bins present (`preferredBin != 0`) + repro | Item is very likely **lot/serial-tracked** — the payload has no lot/serial field to satisfy; needs the `InventoryAssignmentItem` extension + product decision on lot handling |
| NetSuite complains about a different line N than the item we believe caused it | Our index mapping is off — recheck how NetSuite numbers the transformed item list |

### The retest that resolved it: duplicate `orderLine` from query fan-out

The fourth row of the table above predicted a lot/serial item. The field result came back
`vendorBin=0 preferredBin=8 qty=8` — a **valid** row — and the operator's own SuiteQL
confirmed the data NetSuite would need:

```sql
SELECT ib.item, ib.location, ib.binnumber, ib.inventorystatus, ib.quantityavailable
FROM inventorybalance ib
WHERE ib.item = (SELECT id FROM item WHERE itemid='00003619')
  AND ib.location = (SELECT id FROM location WHERE name='OLD WAREHOUSE');
-- 1 row: item 47776, location 7 (OLD WAREHOUSE), bin 8, status 1, 360 available / on hand
```

Bin present, bin belongs to the line's location, quantity available, status matches the
posted `inventoryStatus` 1 (Good). Nothing is missing from the assignment — which means the
failing condition is **not** "this line lacks inventory detail" but **"this line's inventory
detail was overwritten"**.

**Root Cause 2 (the actual `line 108` failure) — the posted body contains the same `orderLine` twice.**

The packing line queries join one transaction line to the item's preferred bin and to the
preferred vendor's bin assignment:

```sql
LEFT JOIN (SELECT ibq.item, ibq.bin, b.location
           FROM itembinquantity ibq JOIN bin b ON b.id = ibq.bin
           WHERE ibq.preferredbin = 'T') ibq ON ibq.item = i.id AND ibq.location = tl.location
```

Neither subquery is deduplicated, so a **single `tl.id` can come back more than once**:

- the item has **two or more bins flagged Preferred at that location** (item 00003619 carries
  12 Bin Numbers rows and was edited the same day), or
- the preferred vendor has **two or more `dbti_vendor_bin_assignment` records** for that
  location (the join keys on vendor only — in the VRA query it keys on vendor with no
  location filter at all, so it fans out per record *and* can attach a bin from another location).

The mobile renders and posts every row it received. The operator scans 8 into one copy; the
other copy stays at zero. The GOOD bucket therefore posted **two `item.items[]` entries with
`orderLine: 536`**:

| body position | payload | effect on the one IF line for TO line 536 |
|---|---|---|
| 108 | `{orderLine:536, itemreceive:true, quantity:8, inventoryDetail:{…bin 8, qty 8}}` | sets quantity 8 and replaces the assignment with bin 8 |
| later | `{orderLine:536, itemreceive:false, inventoryDetail:{inventoryAssignment:{items:[]}}}` | **replaces the assignment with an empty sublist** |

Because the transform URL carries `?replace=item.inventoryDetail.inventoryAssignment`, each
entry's assignment **replaces** the line's sublist rather than merging. The second (empty)
entry wins, leaving the line with a quantity and **no** inventory detail — and NetSuite names
the **first** entry, i.e. the scanned line's position: *"Please configure the inventory detail
in line 108 of the item list"*. This also explains why the missing-bin guard stayed silent
(both copies carry bins) and why Transaction A passed (its items had a single preferred bin
and a single vendor assignment, so no duplicates).

Why this survived the earlier passes: the BAD bucket had a duplicate **guard** (`badDupes`,
added earlier the same day) that *blocked* the save, but the **GOOD and Returns paths had no
guard and no dedupe** — catalog **1b**, spec follow-up **#9**. The PO receipt builder already
groups by `LineSequenceNumber` (`PurchaseOrderIRPayloadDTO.cs:42`); the itemFulfillment path
never got the equivalent.

### The Fix (dedupe + query fan-out)

**1. Service — `DeduplicateFulfillmentLines` (defence in depth, all five posts).**
`SaveTOItemFulfillment`, `SaveReturnsItemFulfillment` and `SaveVRAItemFulfillment` now collapse
each `(LineSequenceNumber, IsBad)` pair to one row before anything else runs:

```csharp
internal static List<T> DeduplicateFulfillmentLines<T>(
    List<T> lines, Func<T, int> lineSequenceNumber, Func<T, bool> isBad, Func<T, decimal> scannedQuantity)
    => lines.GroupBy(x => (lineSequenceNumber(x), isBad(x)))
            .Select(g => g.OrderByDescending(x => scannedQuantity(x) > 0)
                         .ThenByDescending(scannedQuantity)
                         .First())
            .ToList();
```

- Scanned row wins, so the operator's bin and quantity are the ones posted.
- Good/bad split scans of one line (`IsBad` differs) are **kept** — they are legitimate input.
- Quantities are never summed: mobile `Refresh()` restores the scanned quantity onto *every*
  row sharing the key, so two copies can both carry `qty=8`; summing would over-fulfill.
- `GroupBy` preserves first-appearance order, so the posted body order — and the `line N`
  decoder — stay aligned.
- The now-unreachable `badDupes` throw was **removed**: blocking the operator on duplicates is
  the failure mode being fixed, and it only ever guarded one of the five posts.

**2. Queries — stop the fan-out at source** (`NS_TO_x_Packing_Get_Items.sql`,
`NS_TO_x_Return_x_Packing_Get_Items.sql`, `NS_VendorReturnAuthorization_Get_Items.sql`):

- preferred-bin subquery → `GROUP BY ibq.item, b.location` with `MIN(ibq.bin) AS bin`
  (one row per item+location);
- vendor-bin subquery → `GROUP BY iv.item, iv.subsidiary, ba.custrecord_dbti_vba_location`
  with `MIN(ba.custrecord_dbti_vba_assigned_bin)`;
- VRA's `ba` was a bare `LEFT JOIN … ON t.entity = ba.custrecord_dbti_vba_vendor`; it is now a
  `(vendor, location)` grouped subquery **and matches `tl.location`**, so it can neither fan
  out nor attach a bin belonging to another location.

This also removes the duplicate rows from the mobile grid itself, where the same item was
rendered twice and `Refresh()` could push the scanned quantity onto both copies.

**Tests** — `NetSuiteApiClientServiceFulfillmentDedupeTests` (6 cases): the field repro
(scanned copy beats filler copy, bin and qty preserved), unscanned duplicates collapse to the
first row, good/bad split survives, double-scanned duplicates are not summed, first-appearance
order preserved, distinct lines pass through untouched.
Build: `dotnet build Api.CoreWebAPI` → 0 errors. Tests: full `Integration.NS.Tests` green
(`GridResultCacheTests` / `NetSuiteApiClientServiceTokenTests` are timing-sensitive and fail
intermittently under parallel load, in isolation and on the pristine tree alike).

**Data remedy (still owed, outside code):** item 00003619 has 12 Bin Numbers rows and was
edited 15:13 on the day of the failure — confirm whether more than one bin is flagged
**Preferred at OLD WAREHOUSE**, and leave exactly one. `MIN(bin)` makes the resolution
deterministic until then.

### Behaviour Matrix (current code)

| Location | Scanned line | Filler line (qty 0) | Duplicate rows | URL | Status |
|---|---|---|---|---|---|
| Not bin-tracked | `{orderLine, itemreceive:true, quantity}` | `{orderLine, itemreceive:false}` | collapsed to one row | no `replace` | Proven (matches `668cc85e`) |
| Bin-tracked | `+ inventoryDetail.inventoryAssignment[status, qty, bin]` | `+ inventoryDetail.inventoryAssignment.items: []` | **collapsed to one row — the filler copy can no longer empty the scanned line's assignment** | `?replace=item.inventoryDetail.inventoryAssignment` | **Awaiting client repro** |

## Root Cause (confirmed)

1. **Location is bin-tracked** → the transform needs explicit inventory detail per fulfilled line, and `?replace=` so NetSuite replaces its prefilled assignment sublist instead of merging with it.
2. **Payload builders only attach `inventoryDetail` to lines with `ScannedQuantity > 0`.** Zero-qty filler lines (`{orderLine, itemreceive:false}`) had no detail.
3. A GOOD bucket with **1 scanned line out of 25** therefore produced a body where `inventoryDetail` existed on 1 entry only. NetSuite's `replace` validation on the `itemFulfillment` transform reports `'inventoryDetail' ... was not found in the request body`.

Cross-check: the Returns **receipt** (`ReturnsIRPayloadDTO` + `ItemReceiptUrl + "?replace=item.inventoryDetail.inventoryAssignment"`, `NetSuiteApiClientService.cs:1020`) uses the **same filler-line pattern and the same replace param** and works in production. The mix is acceptable on the `itemReceipt` transform; the `itemFulfillment` transform enforces the sub-resource strictly. That difference is why the "same fix" keeps recurring on fulfillment only.

## Investigation path (how it was confirmed)

1. **Initial hypothesis (partly wrong):** the GOOD bucket's guard was `goodTO.Any()` rather than `goodTO.Any(x => x.ScannedQuantity > 0)`, so an all-zero bucket posted with `?replace=` and no detail anywhere. A conditional URL was implemented.
2. **Client retest failed** with the identical error — ruling out stale deployment and refuting the all-zero-bucket theory (the screenshot showed scanned quantities).
3. **Server logs were unavailable** (tested on the client's device/environment), so on-device diagnostics were added: every fulfillment failure now appends `[diag replace=… usedBin=… scannedLines=… totalLines=…]` to the error the mobile toast already shows, plus a full `ILogger.LogError` of the URL and serialized payload server-side.
4. **Client repro returned `[diag replace=True usedBin=True scannedLines=1 totalLines=25]`** — bin-tracked location, detail on 1 of 25 entries, `replace` correctly present. Mixed-coverage root cause confirmed.
5. **Git archaeology** against the original working build (`668cc85e`) showed the "then" payload had no `inventoryDetail` at all, motivating the legacy-path split for non-bin locations.

## The Fix (three parts, all in source)

### 1. Legacy shape for non-bin locations (then-behavior, proven)

`TransferOrderIFPayloadDTO` / `ReturnsIFPayloadDTO` / `VendorReturnAuthorizationIFPayloadDTO` — scanned lines at `!isUsedBin` now emit the exact `668cc85e` shape:

```json
{ "orderLine": n, "itemreceive": true, "quantity": q }
```

no `inventoryDetail`, letting NetSuite auto-manage it. The URL for those posts drops `?replace=` (`ItemFulfillmentUrlNoReplace`).

### 2. Full sub-resource coverage at bin-tracked locations

Zero-qty filler lines at `isUsedBin` now carry an **empty assignment** so the replace target exists on every body entry:

```json
{ "orderLine": n, "itemreceive": false, "inventoryDetail": { "inventoryAssignment": { "items": [] } } }
```

Scanned lines keep the explicit assignment (status, quantity, bin).

### 3. URL selection keyed to payload content (`NetSuiteApiClientService.cs`)

All five fulfillment posts (TO bad/good, Returns, VRA bad/good) choose the URL with the same predicate as the payload shape:

```csharp
// replace + explicit inventoryDetail only when the location is bin-tracked AND the bucket carries scanned lines
var usedBinWithScan = isUsedBin && goodTO.Any(x => x.ScannedQuantity > 0);
var url = string.Format(usedBinWithScan ? ItemFulfillmentUrl : ItemFulfillmentUrlNoReplace, "transferOrder", orderId);
```

`ItemFulfillmentUrlNotUsedBin` was a byte-identical duplicate of `ItemFulfillmentUrl` and is no longer used by the fulfillment paths.

### 4. Diagnostics (temporary, remove after verification)

- On-device suffix: `[diag replace={bool} usedBin={bool} scannedLines={n} totalLines={n}]` appended to every fulfillment failure message.
- Server-side: `logger.LogError(ex, "… Item Fulfillment failed. Url: {Url}. Payload: {Payload}", …)` in all five catch blocks (the class already had an injected `ILogger`).

## Behavior Matrix (current code)

| Location | Scanned line | Filler line (qty 0) | URL | Status |
|---|---|---|---|---|
| Not bin-tracked | `{orderLine, itemreceive:true, quantity}` | `{orderLine, itemreceive:false}` | no `replace` | Proven (matches `668cc85e`) |
| Bin-tracked | `+ inventoryDetail.inventoryAssignment[status, qty, bin]` | `+ inventoryDetail.inventoryAssignment.items: []` | `?replace=item.inventoryDetail.inventoryAssignment` | **Unverified — awaiting client repro** |

Not affected: PO Item Receipt, TO receiving (restlet), Returns Item Receipt — different URLs and payloads, untouched.

## Verification Plan

Build verified locally: `dotnet build Integration.NS` → **0 errors** (pre-existing nullable warnings only). Behavior must be confirmed on the client:

1. **Used-bin STR with partial scans** (the failing case: 1 of 25 lines) — must save. This is the scenario the fix targets.
2. **Used-bin STR with good + bad lines** — both buckets save; verify quantities, bins, and statuses (`C`/`B` per trip-ticket exemption).
3. **Non-bin STR** — legacy path; must behave as in the original working era.
4. **All-zero-qty save** — previously 400; must now post without `replace`.
5. **Returns packing and RTS** — same payload/URL paths.
6. **PO Item Receipt sanity check** — untouched path.

### Reading the next `[diag …]` result

| diag | Meaning | Next action |
|---|---|---|
| Success (no error) | Empty assignment satisfies the replace validation | Remove diags after sign-off |
| `replace=True usedBin=True` + new error about empty/invalid assignment | NetSuite rejects `items: []` on fillers | Next variant: drop `replace` for used-bin and rely on the transform's prefilled detail (i.e. merge-aware payloads), or a zero-qty assignment item — decided with data in hand |
| `replace=True usedBin=True` + same INVALID_PARAMETER | Replace semantics are stricter than modeled | Stop client deploy cycles; replay payloads against a sandbox TO via Postman with 4 variants (replace on/off × detail coverage) |
| `replace=False` on a used-bin post | `IsLocationUsedBin` data gap | Investigate the location flag rather than the payload |

### Pre-test housekeeping

Per the 2026-10-14 RCA: BAD posts before GOOD with no compensation. A failed GOOD post can leave a transfer order partially fulfilled, and a naive retry then fails differently. **Reconcile the affected TO in NetSuite before re-testing**, and prefer a scratch/test transfer order over a live shipment.

## Decisions and Alternatives Rejected

| Decision | Alternatives rejected and why |
|---|---|
| **Restore the `668cc85e` shape for non-bin locations** (chosen) | *Keep detail everywhere*: the `replace` validation then applies to every non-bin post and reintroduces the failure class. |
| **Empty assignment on filler lines at used-bin** (chosen) | *Omit filler lines*: unlisted lines default to being fulfilled — fillers are mandatory. *Zero-qty assignment items*: likely rejected as invalid inventory detail; kept as fallback. |
| **Keep `?replace=` for used-bin** (chosen) | *Drop replace everywhere*: reinstates the "invalid sublist" errors the July fix resolved. |
| **Diagnostics in the thrown message** (chosen) | Server-only logging was useless — the test runs on the client's environment with no log access. |
| **Skip all-zero-qty posts entirely** (deferred) | Business decision (what should a "nothing scanned" save mean?), not a bug fix. |
| **BAD/GOOD compensation** (deferred) | Design decision; needs a pre-post TO status check or post-failure reconciliation. |

## Residual Risk — next likely 4xx

1. ~~High — empty-assignment rejection~~ → **Disproven in field**: Transaction A passed with `items: []` fillers.
2. ~~High — bin-0 lines~~ → **Refuted in field** (2026-10-15): the decoded offender had `preferredBin=8` and SuiteQL confirmed bin 8 holds 360 pcs of that item at the line's location, status Good. The guard stays as a real guard for genuinely bin-less lines, but it was **not** the cause of `line 108`. Lot/serial items remain unsolvable in the payload (`InventoryAssignmentItem` has no lot/serial fields).
3. **High — lot/serial-tracked items.** Still unproven either way: item 00003619 is *not* lot/serial (single `inventorybalance` row, no lot/serial fields on the item record), but if any *other* of the 21 scanned lines is lot-tracked, it will fail the same way after this fix and needs the `InventoryAssignmentItem` extension plus a product decision on lot handling.
4. **Medium — sequence integrity.** BAD-then-GOOD with no compensation leaves partially fulfilled TOs; retries fail confusingly.
5. **Medium — duplicate `orderLine`, good/bad split.** The fan-out case is fixed (#9, #14). The remaining variant is a line deliberately scanned as *both* good and bad, which correctly yields two entries with the same `orderLine` in **two separate POSTS** — accepted. Catalog 1b.
6. **Medium — query fan-out on the receiving side.** The four receiving queries carry the same un-grouped bin joins (#16); their payloads dedupe PO receipts but not Returns/TO receipts, so the same 400 class can surface there.
7. **Low–medium — other modules.** Receipts clean; Inventory Counting / Worksheet untouched and not reported failing.

## Follow-up Actions

| # | Action | Status |
|---|---|---|
| 1 | Conditional `replace` keyed to `isUsedBin && scanned` (5 posts) | **Done — verified in field (Transaction A passed)** |
| 2 | Legacy payload shape for non-bin locations (3 payload builders) | **Done** |
| 3 | Empty assignment on filler lines at used-bin (3 payload builders) | **Done — verified in field (Transaction A passed)** |
| 4 | Payload + URL logging in fulfillment catch blocks | **Done** |
| 5 | On-device `[diag …]` markers | **Done — keep until all flows sign off** |
| 6 | Pre-save bin-resolution guard with named items (catalog 1a) | **Done — awaiting client verification** |
| 7 | Bin data remediation for the flagged items (assign bins in WMS item master) | Open — data, not code |
| 8 | Auto-resolve missing bins from the TO's own inventory detail (SuiteQL) | Deferred — bigger change, better UX |
| 9 | Dedupe `orderLine` in IF builders (catalog 1b) | **Done — `DeduplicateFulfillmentLines` in the 3 fulfillment methods (5 posts), 6 unit tests; supersedes the BAD-only `badDupes` guard, now removed** |
| 10 | BAD/GOOD compensation for partial fulfillment | Deferred — design decision |
| 11 | Close-out semantics for all-zero-qty saves | Deferred — business decision |
| 12 | Reconcile the TOs left partially fulfilled by earlier failed attempts | Pending |
| 13 | `graphify update .` to refresh the knowledge graph (blocked in this session) | Outstanding |
| 14 | Kill the fan-out at source: `GROUP BY` the preferred-bin / vendor-bin joins in the 3 packing queries | **Done — awaiting client verification** |
| 15 | One Preferred bin per item+location in NetSuite item master (data hygiene behind the fan-out) | Open — data, not code |
| 16 | Same fan-out GROUP BY in the **receiving** queries (`NS_TransferOrder_Get_Items.sql`, `NS_TransferOrder_x_Return_Get_Items.sql`, `NS_TO_Get_Itemfulfillment_Items.sql`, `NS_PurchaseOrder_Get_Items.sql`) | Open — same defect family, out of scope for this fix |

## Validation Limits

- Live NetSuite behavior is still unverified for the current fix. The evidence that exists is the on-device diag from the **client's** environment: `replace=True usedBin=True scannedLines=21 totalLines=326` with `line108=WMSLine 536 … vendorBin=0 preferredBin=8 qty=8`, plus the operator's SuiteQL showing 360 pcs of item 47776 at bin 8, OLD WAREHOUSE, status 1.
- **The duplicate-`orderLine` mechanism is inferred, not observed directly.** No server log or NetSuite payload capture confirms that TO line 536 really was fanned out into two rows. The fan-out is provable from the query shape (undeduplicated `itembinquantity` / vendor-bin-assignment joins) and the item's 12 Bin Numbers rows make it likely, but it is the best-supported explanation rather than a measured fact. If the retest still fails at `line N`, re-open this and check the two remaining branches: a lot/serial-tracked item elsewhere in the 21 scanned lines, or NetSuite numbering the IF sublist differently from the posted body position.
- Until a client repro passes, treat the **data** remedy as still owed: confirm item 00003619 has exactly one Preferred bin at OLD WAREHOUSE.
- The claim that the `itemReceipt` transform tolerates the same filler-line mix is based on the Returns IR path being in production use with that payload shape; it was not A/B tested in this session.
- Whether NetSuite accepts `inventoryAssignment.items: []` is unproven — it is the error message's own suggested remedy, applied to full coverage, not a confirmed NetSuite behavior.
