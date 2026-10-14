# SuiteQL Shape Rules — NetSuite Query Constraints for WMS_TALAVERA

**Purpose:** every rule below was paid for with a production incident. When you write
 SuiteQL for `Integration.NS`, check this list before you build it — and when a new
 NetSuite 400 teaches us a rule that is not here, add it (with the incident reference)
 and add a characterization test that enforces it.

**Where to enforce:** the tests in `Integration.NS.Tests/Implementations/Transactions/`
(`GetItemReceiptItemFulfillmentLinesQueryShapeTests`, `GetBarcodeDataPreferredVendorTests`)
and `Integration.NS.Tests/Services/` (`SuiteQLLiteralEscapingTests`, `SuiteQLLimitTests`)
assert the shapes. New fragile queries get the same treatment.

## Rule 1 — No outer-alias references inside subqueries or derived tables

A subquery or derived table must never reference an alias of the outer query
 (`tl`, `uom`, `item`, ...). Resolve ALL correlation inside the derived table itself;
 join the derived table in the ON/OUSIDE predicate using only its own output columns.

```sql
-- REJECTED (correlated scalar subquery referencing outer tl):
... , (SELECT ttl.id FROM previoustransactionlinelink tolink ... WHERE tolink.nextdoc = tl.transaction) AS LineNumber ...

-- ACCEPTED (resolved inside the table, correlated only in the ON clause):
LEFT JOIN (SELECT tolink.nextdoc AS ifdoc, tolink.nextline AS ifline, ttl.id AS tolineid
           FROM previoustransactionlinelink tolink ...) ifmap
       ON ifmap.ifdoc = tl.transaction AND ifmap.ifline = tl.id
```

NetSuite rejects the correlated form with `400 [INVALID_PARAMETER] ... Search error
 occurred: Invalid or unsupported search`.

**Incidents:**
- 2026-10-13 — STR/Returns web item-receipt load: `IF_TO_LINE_ID` (one level of
  correlation) and `IF_TO_LINE_RECEIVED_QTY` (nested two levels, reaching the outer
  `uom` alias) in `ReceivingIntegration.GetItemReceiptItemFulfillmentLinesAsync`.
  Fix: `IF_TO_LINE_MAP` derived table. Only the Item Fulfillment branch was affected,
  which is why the error appeared user-specific — it was actually path-specific
  (STR/Returns web flow vs. the PO flow).
- 2026-10-08 — barcode search: scoping `itemvendor` in the outer join predicate
  (`pv.subsidiary = loc.subsidiary`) against an unscoped derived table was rejected;
  the subsidiary had to be resolved inside the table
  (`SuiteQLFragments.PreferredVendorForLocation`).

**The proven shape:** `LEFT JOIN (derived table) alias ON alias.col = driving.col` —
 the preferred-bin (`pb`) join in `ReceivingIntegration.GetItemReceiptLinesAsync` runs
 this every day in production.

## Rule 2 — Escape every string value interpolated into SQL

`SuiteQLQueryBuilder._stringifyValue` and `_stringOp` double single quotes
 (`'` → `''`). A raw apostrophe in a tranid or a user-typed grid filter terminates
 the string literal early and NetSuite rejects the whole query with
 `400 INVALID_PARAMETER`. Values that must stay raw (fragments) go through
 `SuiteQLQueryBuilder.Literal`, deliberately unescaped.

**Incident class:** recurring near-misses — tranids and vendor names with apostrophes
 reaching `Equal(t.tranid, ...)` unescaped.

## Rule 3 — Limit is `null` or 1..1000

NetSuite rejects `?limit=` outside 1..1000 with `400 INVALID_PARAMETER`. The builder
 clamp (`SuiteQLQueryBuilder.Build`) normalizes Take ≤ 0 → `null` and Take > 1000 →
 1000, so all builder-made queries are safe. Hand-written `ExecuteSuiteQLQuery`
 calls must respect the same bounds.

**Incident:** 2026-10 — User Management sent `Take = int.MaxValue` → `?limit=2147483647`
 → empty employee dropdown, user creation blocked. Fixed by the builder chokepoint.

## Rule 4 — Offset must be divisible by limit, and past-end offsets 404

- NetSuite requires `offset % limit == 0`; `SuiteQLQueryExtensions.ExecuteWithPaging`
  stitches the two legal pages when Radzen produces an unaligned offset.
- An offset past the end of the result set is rejected with
  `404 INVALID_PARAMETER ... 'offset' is out of bounds. Provide value between 0 and N`.
  Paged list reads go through `QueryListWithOffsetClampAsync`, which clamps once to
  the last legal page using the N hint.

**Incidents:** stale Radzen pager positions 404ing on the receiving/packing grids;
 fixed 2026-10 by the offset-clamp retry (`NetSuiteOffsetCorrectedException`).

## Rule 5 — Classification, not string-matching

NetSuite rewords error detail strings without notice. Branch on
 `NetSuiteErrorCode` (`OffsetOutOfBounds`, `UnsupportedSearch`) — never on message
 text. `NetSuiteExceptionFactory.CreateForStatus` appends a query excerpt for
 `UnsupportedSearch` failures; if you hit one, the easiest reproduction is to paste
 that excerpt into NetSuite's SuiteQL console.

## Rule 6 — No trailing comma before a clause keyword or `)`

A comma immediately before `FROM` / `WHERE` / `GROUP BY` / `ORDER BY` / `HAVING`
or a closing paren is a syntax error; NetSuite answers `400 INVALID_PARAMETER`.
It is invisible to every other structural invariant (balanced literals, balanced
parens, no empty `IN`), so it slipped in through two routine edit commits.

```sql
-- REJECTED (comma on the last projected column):
SELECT id AS Id, name AS Name, custrecord_x AS X, FROM location

-- ACCEPTED:
SELECT id AS Id, name AS Name, custrecord_x AS X FROM location
```

**Incidents:**
- 2026-10 — `NS_Get_Helpers.sql` (commit `ae35a10`) and `NS_Get_Locations.sql`
  (commit `4b062dc`) each gained a trailing comma on the last projected column,
  breaking the Trip Ticket helper lookup and the Inventory Counting location list.

**Enforced by:** `SuiteQLAssert.NoTrailingComma` (also folded into `WellFormed`)
and `NSScriptsWellFormedTests`, which scans the real `NSScripts/*.sql` files —
the resolver tests substitute against fakes and never validated the shipped
scripts, which is how this shipped twice.

## Adding a new rule

1. Reproduce with the query text from the warning log
   (`NetSuiteApiClientService.LogNonSuccessResponse` logs the full request body).
2. Confirm in the NetSuite SuiteQL console (or trust the 400 wording if no console
   access).
3. Write the rule here with the approved/ rejected shapes.
4. Add a characterization test that fails when the bad shape reappears.
