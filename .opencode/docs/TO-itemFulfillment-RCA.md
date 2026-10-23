# RCA & Fix Brief — Transfer Order → Item Fulfillment POST fails ("configure the inventory detail")

> **Resolution status (2026-10-23).** F1, F3 and F4 are implemented; F2 is
> implemented as "complete the payload, then assert set equality per bucket"
> instead of "block on a stale load"; F5 stays the documented fallback and is
> out of scope. Acceptance §8 is covered by unit tests where it can be, but
> V1-V4, the sandbox U2 matrix and the replay of the two failing orders remain
> unverified. See `TO-itemFulfillment-resolution-plan.md` and
> `changes/2026-10-23-to-if-reconciliation.md`.
>
> Correction to §4.1: `tl.units` is the key the join is on, so `NVL` only fires
> when `tl.units IS NULL`. A non-null unit always has a `unitstypeuom` row, so
> the fallback cannot mask a real conversion rate.
>
> **Audience:** an LLM/coding agent assisting the developer who owns the WMS ↔ NetSuite integration.
> **Goal:** confirm the root cause with the verification queries below, then implement the fix tasks so that every open TO line is explicitly addressed in the Item Fulfillment payload.
> **Status:** root cause is strongly indicated by the evidence; the exact filter that drops lines is **not yet confirmed** (part of the source query was not available — see §6).

---

## 1. System context

- A handheld WMS app picks Transfer Orders (TOs) and posts an Item Fulfillment (IF) with status **Packed** (`shipStatus: "B"`) to NetSuite via REST.
- Source locations are bin-tracked (e.g. `OLD WAREHOUSE`), so every fulfilled line needs `inventoryDetail.inventoryAssignment` with a bin.
- The app loads TO lines with a SuiteQL query (§5), shows them to the picker, and on save builds the payload from that line list.

### 1.1 Endpoint

```
POST /services/rest/record/v1/transferOrder/{orderId}/!transform/itemFulfillment?replace=item.inventoryDetail.inventoryAssignment
```
(No `?replace=` when the location is not bin-tracked.)

### 1.2 Payload shape (bin-tracked location, one scanned line)

```json
{
  "shipStatus": "B",
  "custbody_dbti_print_label": true,
  "item": {
    "items": [
      {
        "orderLine": 10234,
        "itemreceive": true,
        "quantity": 5,
        "inventoryDetail": {
          "inventoryAssignment": {
            "items": [
              { "inventoryStatus": { "id": "1" }, "binNumber": { "id": "4821" }, "quantity": 5 }
            ]
          }
        }
      }
    ]
  }
}
```

### 1.3 The "filler" rule (existing design, correct)

`!transform/itemFulfillment` **pre-fills the item sublist with the full open quantity** of every open line. A line not addressed in the body is fulfilled anyway. So every open line must be addressed; unscanned lines are sent as **`{ "orderLine": <id>, "itemreceive": false }`** ("fillers").

---

## 2. Observed failures

Both are HTTP 400 `USER_ERROR` with the app's own diagnostic suffix:

| # | TO | Error item | App diagnostic |
|---|---|---|---|
| 1 | `ISTR-THM00100000771` | HAPEE OUTRAGEOUS BLUE CHILL 90MLX2 (item code `00045638`) | `replace=True usedBin=True scannedLines=2 totalLines=4` · `item=… (not matched to a posted line) candidates=[none among 4 posted entries]` |
| 2 | `ISTR-THM00100000776-2` | BONAKID 3/400G(1.2KG) (item code `00004563`) | `replace=True usedBin=True scannedLines=2 totalLines=713` · `item=… (not matched to a posted line)` |

Full NetSuite message:
```
Error while posting GOOD TO Item Fulfillment. NetSuite 400 Bad Request:
[USER_ERROR] Error while accessing a resource. Please configure the inventory detail for <ITEM NAME>.
```

**Key reading of the diagnostic:** the failing item is *not* one of the lines the app posted. NetSuite is rejecting a line the app never sent.

---

## 3. Evidence from the TO exports (saved-search CSVs)

| | TO 771 (HAPEE) | TO 776-2 (BONAKID) |
|---|---|---|
| Distinct item lines on the TO | **63** | **1,657** |
| Lines the app knew about (`totalLines`) | **4** | **713** |
| Position of the failing item | line #35 of 63 | line #1465 of 1,657 |
| Lines the app *did* load (control) | `00035942` SURF PWDR SF 65G 6+1 (line #5), `00045621` SURF FC BF 69ML 6+1 (line #29) | — |

Additional facts:
- All lines in both TOs are `itemType = InvtPart` → item type is **not** the differentiator.
- Each item appears on exactly one TO line → duplicate items are **not** the cause.
- In the saved search, **every TO item line appears as exactly 3 rows** (one TO item line ⇒ multiple `transactionline` records). The query must select the exact line that IF `orderLine` references.
- Only 2 of 1,657 items in TO 776-2 have a blank barcode: `00020388` DP PINOY KURAT/SPCE 375M (line #1423) and `00004563` BONAKID 3/400G (line #1465). Noted, but **not** the general cause — HAPEE has a barcode (`14800034607748`) and fails the same way.
- Caveat: the CSVs have no quantity/shipped/closed columns, so some of the "missing" lines may legitimately be fully shipped or closed. The failing ones were evidently still open.
- Naming caveat: NetSuite error text uses the item **display name** ("HAPEE OUTRAGEOUS BLUE CHILL 90MLX2"), while the line memo/description differs ("HAPEE O.BLUE90MLX2"). Name-based matching in diagnostics is fragile.

---

## 4. Root cause

**Primary:** the set of lines the app sends fillers for comes from the same SuiteQL query that drives the picking UI. That query returns fewer lines than NetSuite considers open. Each open line it misses keeps the transform's defaults (`itemreceive: true`, full open quantity, **no inventory detail**). At a bin-tracked location, NetSuite rejects the first such line with "Please configure the inventory detail for {item}".

**Structural flaw:** display concerns (UoM, preferred bin, vendor bin, stock availability, and so on) and correctness concerns ("which lines must be addressed") share one query. Any filter added for display silently removes lines from the filler set.

### 4.1 Suspects in the query, most likely first

1. **`JOIN unitstypeuom uom ON tl.units = uom.internalid` is an INNER JOIN.** Lines with `tl.units IS NULL` (items without a Units Type, or with incomplete setup) are dropped.
2. **Unseen joins and the WHERE clause.** The SELECT references `ibi1, ibi2, ibv1, ibv2, ibli1, ibli2, ail`, but those joins and the WHERE clause were not included in the snippet. Any INNER JOIN on bin or stock balance, or a filter such as `available > 0`, `quantitybackordered = 0`, or `committed > 0`, drops lines that the transform may still pre-fill (this depends on the account's *Fulfill Based on Commitment* preference).
3. **Stale snapshot.** Lines added or reopened after the picker loaded the TO (the `-2` suffix suggests a split or copied TO) won't get fillers unless the app re-queries right before posting.
4. **Secondary defect, the `ivb` join is missing location.** The subquery groups by `custrecord_dbti_vba_location` but joins only on `item + subsidiary`. A vendor with bin assignments at more than one location produces duplicate rows, which leads to duplicate `orderLine` entries or a dedupe step that picks the wrong row.
5. **Line identity.** With 3 `transactionline` rows per TO item, if the WHERE clause doesn't pin the exact line that IF `orderLine` references, the app may send an `orderLine` that matches no pre-filled line. The real line then stays at its defaults.

---

## 5. Source query (as provided — TRUNCATED)

> The original paste ends after the `ivb` join. Missing: the joins for `ibi1, ibi2, ibv1, ibv2, ibli1, ibli2, ail` and the entire `WHERE` clause. **Get the full query from the codebase before concluding.**

```sql
SELECT
    t.id AS NetsuiteOrderInternalId,
    t.tranid AS OrderNumber,
    t.recordtype AS OrderType,
    t.status AS OrderStatus,
    tl.location AS NetsuiteFromLocationInternalId,
    t.transferlocation AS NetsuiteToLocationInternalId,
    t.subsidiary AS NetsuiteFromSubsidiaryInternalId,
    s.name AS FromSubsidiaryName,
    s.custrecord_dbti_default_bo_location AS NetsuiteSubsidiaryDefaultBOInternalId,
    t.tosubsidiary AS NetsuiteToSubsidiaryInternalId,
    loc.name AS LocationName,
    loc.usebins AS LocationUsedBin,
    tl.id AS LineSequenceNumber,
    tl.transactionlinetype AS TransactionLineType,
    tl.item AS NetsuiteMaterialInternalId,
    i.itemid AS MaterialCode,
    i.displayname AS MaterialName,
    tl.custcol_dbti_vendor AS VendorId,
    BUILTIN.DF(tl.custcol_dbti_vendor) AS VendorName,
    b.id AS NetsuiteMaterialPrefferedBinId,
    ibi1.quantityavailable AS PreferredBinQuantityAvailableGood,
    ibi2.quantityavailable AS PreferredBinQuantityAvailableBad,
    ivb.custrecord_dbti_vba_assigned_bin AS NetsuiteMaterialVendorAssignedBin,
    ibv1.quantityavailable AS VendorAssignedBinQuantityAvailableGood,
    ibv2.quantityavailable AS VendorAssignedBinQuantityAvailableBad,
    ibli1.quantityavailable AS LocationItemQuantityAvailableGood,
    ibli2.quantityavailable AS LocationItemQuantityAvailableBad,
    i.weight AS MaterialWeight,
    ail.quantityavailable as LocationItemQuantityAvailable,
    ABS(tl.quantity) AS LineQuantity,
    tl.quantitypacked AS LineQuantityPacked,
    tl.quantitybackordered AS LineQuantityBackOrdered,
    tl.isclosed AS IsClosed,
    tl.quantityshiprecv AS QuantityAlreadyShipped,
    tl.units AS NetsuiteUoMInternalId,
    uom.unitname AS UoMName,
    uom.conversionrate AS UoMRate,
    TO_CHAR(t.createddate, 'YYYY-MM-DD"T"HH24:MI:SS') AS NetsuiteOrderCreatedDate
FROM item i
JOIN transactionline tl ON i.id = tl.item
JOIN transaction t ON tl.transaction = t.id
JOIN subsidiary s ON t.subsidiary = s.id
JOIN location loc ON tl.location = loc.id
JOIN unitstypeuom uom ON tl.units = uom.internalid          -- SUSPECT #1 (INNER)
LEFT JOIN (
    SELECT ibq.item, MIN(ibq.bin) AS bin, b.location
    FROM itembinquantity ibq
    INNER JOIN bin b ON b.id = ibq.bin
    WHERE ibq.preferredbin = 'T'
      AND ibq.item IN (SELECT txl.item FROM transaction tx
                       JOIN transactionline txl ON txl.transaction = tx.id
                       WHERE tx.tranid = @tranid)
    GROUP BY ibq.item, b.location
) ibq ON ibq.item = i.id AND ibq.location = tl.location
LEFT JOIN bin b ON b.id = ibq.bin
LEFT JOIN (
    SELECT iv.item, iv.subsidiary,
           MIN(ba.custrecord_dbti_vba_assigned_bin) AS custrecord_dbti_vba_assigned_bin,
           ba.custrecord_dbti_vba_location
    FROM itemvendor iv
    JOIN customrecord_dbti_vendor_bin_assignment ba ON iv.vendor = ba.custrecord_dbti_vba_vendor
    WHERE iv.preferredvendor = 'T'
      AND iv.item IN (SELECT txl.item FROM transaction tx
                      JOIN transactionline txl ON txl.transaction = tx.id
                      WHERE tx.tranid = @tranid)
    GROUP BY iv.item, iv.subsidiary, ba.custrecord_dbti_vba_location
) ivb ON ivb.item = i.id AND ivb.subsidiary = t.subsidiary   -- SUSPECT #4 (no location)
-- ... TRUNCATED: joins for ibi1/ibi2/ibv1/ibv2/ibli1/ibli2/ail and WHERE clause not provided
```

Also note: `tranid` is not guaranteed unique across records. Prefer filtering subqueries by `transaction.id`.

---

## 6. Verification steps (do these first)

### V1 — Diff the failing lines against a control line
Whichever column differs between the failing items and the control is the filter that drops them.

```sql
SELECT t.tranid, tl.id AS line_id, i.itemid, i.displayname,
       tl.location, tl.units, tl.quantity, tl.quantityshiprecv,
       tl.quantitycommitted, tl.quantitybackordered, tl.isclosed,
       tl.transactionlinetype, tl.mainline
FROM transactionline tl
JOIN transaction t ON t.id = tl.transaction
JOIN item i ON i.id = tl.item
WHERE t.tranid IN ('ISTR-THM00100000771','ISTR-THM00100000776-2')
  AND i.itemid IN ('00045638','00004563','00020388','00035942')
ORDER BY t.tranid, i.itemid, tl.id
```
`00035942` is the control (it was loaded by the app). If a column errors in SuiteQL, drop it and continue.

### V2 — Bisect the app query
Run the full app query for `ISTR-THM00100000771`. Expect 4 rows. Convert the INNER JOINs to LEFT JOINs and relax WHERE predicates one at a time until `00045638` appears. Record which change brings it back.

### V3 — Confirm which TO line `orderLine` references
On an IF from a TO that posted successfully:
```sql
SELECT previousdoc, previousline, nextdoc, nextline
FROM NextTransactionLineLink
WHERE nextdoc = <successful IF internal id>
```
Check that `previousline` values equal the `LineSequenceNumber` the app sends, and not one of the other `transactionline` rows of the same item.

### V4 — Check the commitment preference
Check *Setup › Accounting › Accounting Preferences › Order Management › Fulfill Based on Commitment*. If it's "Ignore Commitment", the transform pre-fills lines that have no committed stock, so any filter on stock or commitment in the app query will leave those lines without fillers.

---

## 7. Fix tasks

### F1 — Separate the filler set from the display query (required)
- Add a dedicated query, **"open fulfillable lines"**, scoped by `transaction.id`. It returns exactly the TO item lines that IF `orderLine` references, where remaining quantity > 0 and `isclosed = 'F'`.
- **No joins** to UoM, bin, stock, vendor or custom records.
- Run it **immediately before the POST**, not at screen load.
- Payload = scanned lines (with inventory detail) **plus** `{ "orderLine": id, "itemreceive": false }` for **every** other open line.

### F2 — Pre-flight guard (required)
Before posting, assert `set(payload.orderLine) == set(openFulfillableLines.lineId)`. If they differ, do not post. Re-sync the TO in the app and show the picker which lines changed.

### F3 — Fix the display query (required)
- `JOIN unitstypeuom` → `LEFT JOIN`; treat a NULL `conversionrate` as 1 and show the base unit.
- Add `AND ivb.custrecord_dbti_vba_location = tl.location` to the `ivb` join.
- Change any INNER JOIN on stock or bin balances to LEFT JOIN. Show "0 available" in the UI instead of hiding the line.
- Filter subqueries by `transaction.id`, not `tranid`.

### F4 — Diagnostics (recommended)
On a 400, log and display the set difference between open lines and posted lines (as line IDs and item internal IDs). Stop matching on item names: NetSuite errors use the display name, which differs from line memos.

### F5 — Server-side transform via RESTlet (recommended, most robust)
Move the fulfillment build into a RESTlet:
`record.transform({ fromType: TRANSFER_ORDER, toType: ITEM_FULFILLMENT, ... })` → loop `getLineCount({ sublistId: 'item' })` → for each line, read `orderline`. If it was scanned, set quantity and inventory detail (bin + status); otherwise set `itemreceive = false` → set `shipstatus = 'B'` → `save()`.
This removes any possibility of the client's line list drifting from NetSuite's.

---

## 8. Acceptance criteria

- [ ] V1–V4 have been run and the specific filter that dropped `00045638` and `00004563` has been identified and documented.
- [ ] Re-posting TO `ISTR-THM00100000771` with the same 2 scanned lines succeeds. The resulting IF contains only the scanned lines with `itemreceive = true`, with correct bins and quantities.
- [ ] Same for `ISTR-THM00100000776-2`.
- [ ] A TO containing an item with no Units Type, and one with 0 available stock, both display in the app and post correctly when left unscanned.
- [ ] The pre-flight guard blocks a post when a line is added to the TO after the picker loaded it.
- [ ] No duplicate `orderLine` values are ever sent in a payload.
