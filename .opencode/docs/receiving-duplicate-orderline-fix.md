# Receiving TO — Duplicate `orderLine` in the Restlet Payload

> Status: implemented.
> Date: 2026-10-17.
> Scope: NetSuite Transfer Order (TO) **receiving** payload (`orderLine` uniqueness).
> Related: `inventory-status-resolution-fix.md` (packing sibling fix, 2026-10-15);
> `.github/specs/2026-10-15-if-replace-param-invalid-parameter-fix.md` (line 339, receiving queries marked Open).
> Sibling fix: commit `a6ad1d94` (packing only).

---

## 1. The reported symptom

The mobile TO receiving flow built a restlet body whose `lines[]` contained the **same
`orderLine` twice**, e.g. line `234` and line `249` each appearing once as a scanned row
and once as a zero-quantity filler row. The NetSuite restlet contract requires
`lines[].orderLine` to be unique, so the save failed.

The body pinned to the **mobile** restlet builder, not the web path:

```jsonc
{
  "transferCategory": 5,
  "lines": [
    { "orderLine": 234, "inventoryStatus": 6, "quantity": 6, /* ... */ },
    { "orderLine": 234, "inventoryStatus": 6, "quantity": 0, /* ... */ },
    { "orderLine": 249, "inventoryStatus": 6, "quantity": 4, /* ... */ }
    // ...
  ]
}
```

- `inventoryStatus: 6` (= Missing at this account) is produced only by the mobile builder
  `TransferOrderIRRestletPayloadDTO.CreateForItemReceiptRestlet` (`InventoryStatus = line.IsMissing ? 6 : ...`).
  The web builder would emit `5`, so the reported body is the mobile restlet.
- Duplicate `234` is the fan-out; the two rows differ only in the joined bin columns.

## 2. Root cause — query fan-out mapped 1:1 into the payload

`Integration.NS/NSScripts/NS_TO_Get_Itemfulfillment_Items.sql` joins, on each transaction
line, two *optional* lookups that are **not** constrained to one row per line and had **no
`GROUP BY`**:

| Derived table | Join keys | Multi-row source |
|---|---|---|
| `ibq` | `item` × `location` | `itembinquantity WHERE preferredbin = 'T'` — many preferred bins per item/location |
| `ivb` | `item` × `subsidiary` × `location` (via `itemvendor` → `customrecord_dbti_vendor_bin_assignment`) | many bin-assignment records per vendor |

One transaction line therefore fans into **N rows** (one per matching bin / assignment).
`TransferOrderIRRestletPayloadDTO.CreateForItemReceiptRestlet` maps rows 1:1 into
`lines[]`, so the fan-out became duplicate `orderLine` values in the body.

Contributing factor: `NetSuiteApiClientService.SaveTOItemReceipt` had **no dedupe guard** —
it bucketed and posted `Data` as received.

## 3. Why receiving was missed

The identical defect was found and fixed for **packing** in commit `a6ad1d94`
(`DeduplicateFulfillmentLines` + `GROUP BY`/`MIN` in the packing scripts). The fix was
scoped to packing DTOs/scripts; the receiving path was never given the same treatment.
The 2026-10-15 spec recorded the receiving queries as "same defect family, out of scope".

## 4. Fix applied

Belt-and-suspenders, mirroring the shipped packing pattern: collapse the fan-out at the
**source** and **guard** it at the **service**.

### 4.1 SQL — one row per line (root fix)

`Integration.NS/NSScripts/NS_TO_Get_Itemfulfillment_Items.sql`, both derived tables:

```sql
-- ibq
MIN(ibq.bin) AS bin
...
GROUP BY ibq.item, b.location

-- ivb
MIN(ba.custrecord_dbti_vba_assigned_bin) AS custrecord_dbti_vba_assigned_bin
...
GROUP BY iv.item, iv.subsidiary, ba.custrecord_dbti_vba_location
```

`MIN` picks a deterministic bin when several qualify; the service guard below still prefers
the row the operator actually scanned.

### 4.2 Service — dedupe guard

`Integration.NS/Services/NetSuiteApiClientService.cs`:

- New helper `DeduplicateReceivingLines<T>` (sibling of `DeduplicateFulfillmentLines`).
- Called at the top of `SaveTOItemReceipt`:
  ```csharp
  Data = DeduplicateReceivingLines(
      Data,
      x => x.LineSequenceNumber, x => x.IsBad, x => x.IsMissing, x => x.ScannedQuantity);
  ```

#### The critical nuance: the key MUST include `IsMissing`

The packing helper keys on `(orderLine, IsBad)`. **Receiving cannot reuse that key.**
A partially-scanned line legitimately produces **two** entries with the *same*
`orderLine` **and** the *same* `IsBad = false`:

- a **good** entry (the scanned quantity), and
- a **missing** entry (the unscanned remainder, `IsMissing = true`).

These flow into separate good/missing buckets downstream. Keying on `(orderLine, IsBad)`
alone would silently drop one of them and lose a whole bucket of stock. The receiving key
is therefore `(LineSequenceNumber, IsBad, IsMissing)`, and `GroupBy` preserves
first-appearance order. When a group does contain true duplicates, the scanned row
(`ScannedQuantity > 0`) wins.

### 4.3 Tests

`Integration.NS.Tests/Services/NetSuiteApiClientServiceReceivingDedupeTests.cs`:

| Test | Guarantee |
|---|---|
| `Fanout_scanned_row_wins_over_filler_copy` | fan-out collapses; scanned row's qty/bin survive |
| `Good_and_missing_pair_for_one_line_is_kept` | good + missing of one line BOTH survive (the nuance above) |
| `Good_bad_split_of_one_line_is_kept` | good/bad split of one line preserved |
| `First_appearance_order_is_preserved` | output order matches input order |
| `Distinct_lines_pass_through_untouched` | no-op when there is nothing to collapse |

Run: `dotnet test Integration.NS.Tests --filter "FullyQualifiedName~ReceivingDedupe"`

## 5. Files changed

| File | Change |
|---|---|
| `Integration.NS/NSScripts/NS_TO_Get_Itemfulfillment_Items.sql` | `MIN(...)` + `GROUP BY` on `ibq` and `ivb` |
| `Integration.NS/Services/NetSuiteApiClientService.cs` | `DeduplicateReceivingLines<T>` helper; call in `SaveTOItemReceipt` |
| `Integration.NS.Tests/Services/NetSuiteApiClientServiceReceivingDedupeTests.cs` | new test class (5 tests) |

## 6. Remaining same-family items (NOT fixed here)

These share the fan-out defect and should be addressed in follow-ups:

- **Returns receiving** — `NS_TransferOrder_x_Return_Get_Items.sql` + `SaveReturnsItemReceipt`
  (no dedupe) + `ReturnsIRPayloadDTO.CreateForItemReceipt` (maps rows 1:1). Same shape as TO receiving.
- **Web TO receiving** — `ReceivingIntegration.GetItemReceiptItemFulfillmentLinesAsync` /
  `GetItemReceiptLinesAsync` / `CreateTOJson`; may be partly self-filtering via the
  `InventoryDetails` check, but the source fan-out is unchanged.
- **Other receiving scripts** — `NS_TransferOrder_Get_Items.sql`, `NS_PurchaseOrder_Get_Items.sql`
  (listed in the 2026-10-15 spec), same `ibq`/`ivb` pattern.

## 7. How to re-verify

1. `dotnet test Integration.NS.Tests --filter "FullyQualifiedName~ReceivingDedupe"` → 5 passed.
2. Against NetSuite, receive a TO line whose item has **multiple preferred bins** at the
   location (or a vendor with multiple bin assignments): confirm the posted body has a
   unique `orderLine` per (quality, missing) bucket.
3. Regression: receive a line with a **partial** scan and confirm NetSuite still records
   both the good and the missing quantity.
