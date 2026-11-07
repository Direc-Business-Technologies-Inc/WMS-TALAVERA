# NetSuite Purchase Classification Reference

## Evidence and verification status

Recorded on 2026-11-07 from user-supplied NetSuite screenshots, SuiteQL results,
and a Field Explorer export for inventory item `13643`. These are observed
account values, not an exhaustive or immutable enumeration for application code.
Do not hardcode these lookup values into item filtering.

The category/subcategory reference data and item mapping are **verified** by the
supplied SuiteQL results and custom item field definition:

| Field property | Confirmed value |
| --- | --- |
| Label | Item Purchase Subcategory |
| Script ID | `custitem_dbti_item_group` |
| Custom field internal ID | `4950` |
| Type | List/Record |
| List/Record target | Purchase Subcategory |
| Store Value | Enabled |
| Applies to Inventory Item | Enabled |

Join `item.custitem_dbti_item_group` to
`CUSTOMRECORD_DBTI_PURCHASE_CATEGORIES.id`, then derive Purchase Category from
`custrecord_dbti_psc_purchase_category`. No separate item-level Purchase Category
field is needed. The successful query for item `13643` returned Food (`1`) under
Trade (`1`).

## Purchase categories

Source: `CUSTOMLIST_DBTI_PURCHASE_CATEGORY_LIST`.

| Internal ID | Name |
| --- | --- |
| 1 | Trade |
| 3 | Non-trade |

```sql
SELECT id, name
FROM CUSTOMLIST_DBTI_PURCHASE_CATEGORY_LIST
ORDER BY id
```

## Purchase subcategories

Source: `CUSTOMRECORD_DBTI_PURCHASE_CATEGORIES`.
Parent category field: `custrecord_dbti_psc_purchase_category`.

| Internal ID | Name | Parent category ID | Parent category name |
| --- | --- | --- | --- |
| 1 | Food | 1 | Trade |
| 2 | Non-Food | 1 | Trade |
| 3 | Consumables | 3 | Non-trade |
| 4 | Office Supplies | 3 | Non-trade |
| 5 | Store Supplies | 3 | Non-trade |
| 6 | Fixed Assets | 3 | Non-trade |
| 7 | Fund Transfer | 3 | Non-trade |
| 8 | Services | 3 | Non-trade |
| 9 | Uniforms | 3 | Non-trade |

```sql
SELECT
    id,
    name,
    custrecord_dbti_psc_purchase_category AS purchase_category_id,
    BUILTIN.DF(custrecord_dbti_psc_purchase_category) AS purchase_category_name
FROM CUSTOMRECORD_DBTI_PURCHASE_CATEGORIES
ORDER BY custrecord_dbti_psc_purchase_category, id
```

## Sample inventory item

| Property | Observed value |
| --- | --- |
| Record type | `inventoryitem` |
| Internal ID | `13643` |
| Item number (`itemid`) | `00009886` |
| Display name | MAGGI MAGIC SARAP SEASONING GRANULES 8G |
| Purchase Subcategory (UI) | Food |
| Subsidiary (UI / raw ID) | TGC / `1` |
| `custitem_dbti_item_group` | `1` — verified Purchase Subcategory reference (Food) |
| Item Category 1 (UI) | FOOD 102 |
| `custitem_dbti_item_category_1` | `3` |
| Item Category 2 (UI) | CONDIMENTS & SEASONINGS 10010 |
| `custitem_dbti_item_category_2` | `10010` |
| Base unit (UI / raw ID) | PC / `1` |
| Primary stock unit (UI / raw ID / conversion rate) | PC / `1` / `1` |
| Primary purchase unit (UI / raw ID / conversion rate) | BOX960 / `196` / `960` |
| Primary sale unit (UI / raw ID / conversion rate) | PC1 / `16` / `1` |
| UPC (`upccode`) | `48025522` |
| Uses bins | Yes (`usebins: "T"`) |
| Loose Item | No (`custitem_dbti_loose_item: "2"`) |
| POS Item | Yes (`custitem_dbti_pos_item: "1"`) |
| POS Weighted Item | No (`custitem_dbti_pos_weighted_item: "2"`) |

**Do not confuse Item Category 1 with Purchase Category.** In this example,
Item Category 1 has internal ID `3`, while Food belongs to Purchase Category `1`
(Trade). The numerical overlap with Purchase Category `3` (Non-trade) does not
establish a relationship between those separate classifications.

The raw Field Explorer dump is intentionally not reproduced. Only relevant
reference values are retained; session metadata and changing stock/cost values
are not part of this mapping reference.

## Reproducing item-field verification

1. Open inventory item `13643` and inspect the `Purchase Subcategory` field with
   Field Explorer or its field help.
2. In **Customization → Lists, Records, & Fields → Item Fields**, inspect
   `custitem_dbti_item_group`. Verify its label and List/Record target. The target
   must be the purchase-subcategory custom record above for the proposed join.
3. Run this read-only diagnostic query:

```sql
SELECT
    i.id,
    i.itemid,
    i.displayname,
    i.custitem_dbti_item_group AS item_group_id,
    BUILTIN.DF(i.custitem_dbti_item_group) AS item_group_name,
    sc.id AS purchase_subcategory_id,
    sc.name AS purchase_subcategory_name,
    sc.custrecord_dbti_psc_purchase_category AS purchase_category_id,
    BUILTIN.DF(sc.custrecord_dbti_psc_purchase_category) AS purchase_category_name
FROM item i
LEFT JOIN CUSTOMRECORD_DBTI_PURCHASE_CATEGORIES sc
    ON sc.id = i.custitem_dbti_item_group
WHERE i.id = 13643
```

Confirmed result: subcategory `1` (`Food`) and category `1` (`Trade`), matching
the item UI and custom field definition. When checking another environment,
verify its List/Record target as well as query results; matching numeric IDs alone
do not prove the relationship. A SuiteQL error may indicate role permissions or
field exposure differ in that environment.

## WMS implementation requirements

Confirmed by the user:

- Require both Purchase Category and Purchase Subcategory before item entry.
- Filter item selection by both classifications, preserving existing location,
  subsidiary, availability, and preferred-vendor restrictions as applicable.
- Apply the same classification eligibility to barcode entry.

Affected creation flows:

| Flow | Creation route | Shared form |
| --- | --- | --- |
| Transfer Order | `/transactions/stocktransferrequest/Create?type=transferorder` | `STRForm` |
| Intercompany Transfer Order | `/transactions/stocktransferrequest/Create?type=intercompanytransferorder` | `STRForm` |
| Returns | `/transactions/stocktransferrequest/Create?type=returns` | `STRForm` |
| Manual Return to Supplier | `/transactions/returns-to-supplier/create` | `SupplierReturnForm` |

Item grids filter by the verified subcategory field and derived category before
paging. Both shared forms require the selections, check picker results and
barcodes, and re-read line classifications before manual submission. Changing
either classification prompts to clear lines, scans, and row selection. STR
barcode verification allows new matching items within available quantity; RTS
retains its preferred-vendor and On Hand checks. PO-derived RTS keeps its existing
read-only item-entry workflow.

Transaction header fields `custbody_dbti_purchase_category` and
`custbody_dbti_purchase_subcategory` remain separate from these item mappings.

Existing query references:

- `Integration.NS/Implementations/Transactions/SupplierReturnIntegration.cs`:
  purchase-category and subcategory lookup queries.
- `Integration.NS/Implementations/Others/ItemsIntegration.cs`: item-grid mapping
  and query path.
- `Integration.NS/Implementations/Transactions/ReceivingIntegration.cs`: barcode
  item classification mapping.
- `Web.BlazorServer/Helpers/PurchaseClassificationItemRules.cs`: shared item
  eligibility, filters, and submission revalidation.
