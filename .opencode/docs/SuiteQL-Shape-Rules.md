# SuiteQL Shape Rules — NetSuite Query Constraints for WMS_TALAVERA

**Purpose:** every rule below was paid for with a production incident. When you write
 SuiteQL for `Integration.NS`, check this list before you build it — and when a new
 NetSuite 400 teaches us a rule that is not here, add it (with the incident reference)
 and add a characterization test that enforces it.

**Where to enforce:** the tests in `Integration.NS.Tests/Implementations/Transactions/`
(`GetItemReceiptItemFulfillmentLinesQueryShapeTests`, `GetBarcodeDataPreferredVendorTests`)
and `Integration.NS.Tests/Services/` (`SuiteQLLiteralEscapingTests`, `SuiteQLLimitTests`,
`SuiteQLTextCommentTests`) assert the shapes. New fragile queries get the same treatment.

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
   this every day in production. An `INNER JOIN` onto a derived table serves the same
   purpose when the derived table is an *existence* test: `SuiteQLFragments.PendingPackingLines`
   (transactions that still have an open line with quantity left to pack) is inner-joined
   on the transaction id, so a fully packed order drops out of the packing lists. Write it
   this way, not as `EXISTS (... WHERE sub.transaction = t.id)`.

**A "still has work to do" gate cannot be expressed as an order-state predicate.** Both
   order-state columns available on `transaction` — `ordpicked` (picking) and `status`
   (the order workflow) — move on their own schedules. A packing screen that filters on
   them alone keeps offering orders that are already fulfilled, because fulfillment is a
   *line* state (`transactionline.quantitypacked` / `quantityshiprecv`) and packing is a
   separate step from shipping. Gate on remaining line quantity instead.

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

## Rule 7 — No SQL comments in a named script

Every SuiteQL statement is flattened before it leaves the process.
`NetSuiteApiClientService.FormatQuery` applies `Regex.Replace(query, @"\s+", " ")`,
which folds newlines into spaces. A `--` line comment in a multi-line script
therefore stops meaning "comment until the next newline" and starts meaning
"comment until the end of the statement", silently deleting every remaining join,
the `WHERE` clause, and the closing parenthesis of any derived table.

NetSuite answers with an error that names no offending token:

```text
400 [INVALID_PARAMETER] ... Failed to parse SQL [...]: end of input, state:96(10102) near:  (0,0, token code:0)
no_root_node(-1000) near: no root node(0,0, token code:0)
```

`end of input` + `no_root_node` means "I reached the end of the statement and
never found a parse root" — the signature of a comment having eaten the
remainder. Do not read it as a bad column or a bad join; it is neither.

```sql
-- REJECTED (a -- comment anywhere in a named script):
SELECT q.itemid AS ItemCode
FROM item i
JOIN transactionline tl ON i.id = tl.item
-- one row per (item, location) via GROUP BY on a MIN()
LEFT JOIN bin b ON b.id = tl.item
WHERE tl.transactionlinetype = 'SHIPPING' AND t.tranid = @tranid

-- ACCEPTED (same query, no comment; rationale lives in the consuming C#):
SELECT q.itemid AS ItemCode
FROM item i
JOIN transactionline tl ON i.id = tl.item
LEFT JOIN bin b ON b.id = tl.item
WHERE tl.transactionlinetype = 'SHIPPING' AND t.tranid = @tranid
```

**Comments are now stripped, not merely forbidden.** `SuiteQLText.StripComments`
removes `--` and `/* */` before the collapse, so a comment can no longer break a
query. The rule remains because stripping makes commented-out SQL **vanish
silently** — you will get no error telling you the clause you thought you were
testing is not running. Keep query rationale in the XML doc of the method that
consumes the script, and never comment out SQL in a script you intend to run.

**Incidents:**

- 2026-10-16 — `NS_TO_x_Packing_Get_Items.sql`, `NS_TO_x_Return_x_Packing_Get_Items.sql`
  and `NS_VendorReturnAuthorization_Get_Items.sql` each carried a `--` explanation
  block from the October preferred-bin / item-fulfillment dedup work. The three
  packing line grids failed with `end of input ... no_root_node`. They were the only
  scripts in `NSScripts` with comments, which is why nothing else broke. Web and
  mobile were both affected (`FormatQuery` runs on both call paths).
  Change record: `.opencode/docs/suiteql-comment-stripping-fix.md`.

**Ruled out — do not re-investigate:** `ORDER BY` inside a `LEFT JOIN`ed derived
table is accepted by NetSuite. It was removed while diagnosing the above incident
and restored on 2026-10-16; the query succeeds either way. It is a no-op for the
join result, but it is not a parse error.

**Enforced by:** `SuiteQLAssert.HasComment`, plus
`SuiteQLTextCommentTests.No_shipping_script_carries_a_sql_comment`, which scans the
real `NSScripts/*.sql`. Coverage currently extends to `.sql` files only — see the
open items in the change record.

## Adding a new rule

1. Reproduce with the query text from the warning log
   (`NetSuiteApiClientService.LogNonSuccessResponse` logs the full request body).
2. Confirm in the NetSuite SuiteQL console (or trust the 400 wording if no console
   access).
3. Write the rule here with the approved/ rejected shapes.
4. Add a characterization test that fails when the bad shape reappears.
