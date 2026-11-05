# Packing line queries — scope the aggregates to the transfer order

Date: 2026-10-20
Area: `Integration.NS/NSScripts` — `NS_TO_x_Packing_Get_Items.sql`,
`NS_TO_x_Return_x_Packing_Get_Items.sql`, `NS_VendorReturnAuthorization_Get_Items.sql`
Type: performance (map/reduce of the SuiteQL cost), no behavioural change

## Problem

The three packing line scripts answer a question about **one** transfer order
(`t.tranid = @tranid`, the last line of each script), but every aggregate derived
table in them was **unscoped by that order**. NetSuite had to aggregate whole
tables to then join the result down to the handful of items on the transfer and
throw the rest away.

Cost map, worst first:

| Derived table | Source | Scope before | Feeds |
|---|---|---|---|
| `ibli1` | `inventorybalance` | none (full `SUM … GROUP BY`) | `LocationItemQuantityAvailableGood` |
| `ibli2` | `inventorybalance` | none (same aggregate, run again) | `LocationItemQuantityAvailableBad` |
| `ail` | `AggregateItemLocation` | none + 2 unused `BUILTIN.DF()` + a discarded `ORDER BY` | `LocationItemQuantityAvailable` |
| `ibq` | `itembinquantity` × `bin` | none (full `GROUP BY`) | preferred bin, `PreferredBinQuantityAvailable*` |
| vendor-bin (`ivb` in STR, `ba` in Returns/VRA) | `itemvendor` × vendor-bin custom record | none (full `GROUP BY`) | vendor bin, `VendorAssignedBinQuantityAvailable*` |

`inventorybalance` is one row per item/bin/status and is normally the largest
table in a WMS account, so the two `ibli` aggregates were the dominant cost.

The four remaining `inventorybalance` joins (`ibv1`, `ibv2`, `ibi1`, `ibi2`) were
already keyed on item + location + bin + status in their `ON` clause. They were
left alone.

## Root cause

The `WHERE t.tranid = @tranid` predicate sits on the outer query. A derived table
in `LEFT JOIN (SELECT …)` cannot see it, and Rule 1 of `SuiteQL-Shape-Rules.md`
forbids making it see it (no outer-alias references — NetSuite answers with
`400 INVALID_PARAMETER`). Nothing pushed the order identity into the aggregates.

## What changed

Each aggregate derived table gained a self-contained item filter:

```sql
WHERE item IN (
    SELECT txl.item
    FROM transaction tx
    JOIN transactionline txl ON txl.transaction = tx.id
    WHERE tx.tranid = @tranid
)
```

Applied to:

| Script | `ibq` | vendor-bin | `ail` | `ibli1` | `ibli2` |
|---|---|---|---|---|---|
| `NS_TO_x_Packing_Get_Items.sql` | yes | yes (`ivb`) | yes | yes | yes |
| `NS_TO_x_Return_x_Packing_Get_Items.sql` | yes | yes (`ba`) | yes | yes | yes |
| `NS_VendorReturnAuthorization_Get_Items.sql` | yes | **no** | yes | yes | yes |

The vendor-bin table is deliberately skipped in VRA: that script's `ba` is grouped on
`customrecord_dbti_vendor_bin_assignment` by `(vendor, location)` and carries no
`item` column, so it cannot be item-scoped. Its cardinality is bounded by the
vendor count, not the item count.

Also removed from the `ail` derived table in all three scripts:

- `BUILTIN.DF( item ) AS itemname` and `BUILTIN.DF( location ) AS locationname` —
  **dead projections**. The outer `SELECT` only reads `ail.quantityavailable`
  (STR `:38`, Returns `:39`, VRA `:36`), so both `BUILTIN.DF()` calls were
  formatting values nobody consumed.
- `ORDER BY Item, Location` — discarded by the join. `SuiteQL-Shape-Rules.md`
  records that NetSuite accepts it and that it is a no-op for the join result;
  it is removed here because it was doing nothing, not because it was a fault.

### Why the filter shape is safe

1. **No outer-alias reference.** `tx` and `txl` are introduced inside the
   subquery and are not outer aliases, so
   `SuiteQLAssert.NoOuterAliasReferenceInDerivedTable` passes (Rule 1).
2. **`IN`, not `EXISTS`.** `SuiteQL-Shape-Rules.md` records that the correlated
   `EXISTS (… WHERE sub.transaction = t.id)` form is rejected. `IN (self-contained
   SELECT)` is the accepted shape.
3. **`IN`, not a join inside the subquery.** Joining `inventorybalance` to
   `transactionline` would multiply balance rows when one item appears on several
   lines and `SUM` would over-count. `IN` is set membership — no fan-out.
4. **The filter is a superset of what the join needs.** It restricts by item only,
   not by location or line type, and the outer query already restricts to
   `t.tranid`. Every item the join could match is on the transfer, so the filter
   cannot remove a row the join would have produced. It only discards aggregates
   for items that were going to be dropped anyway.
5. **`@tranid` reuse is safe.** `SuiteQLText.SubstituteScriptParameters`
   (`SuiteQLText.cs:52`) performs a global `string.Replace`, so every occurrence
   is substituted with the escaped literal (`SuiteQLText.cs:33`).

## What was deliberately NOT changed

These are load-bearing decisions from earlier fixes. Any future edit to these
scripts must preserve them:

1. **`ibq` / `ba` keep `GROUP BY` + `MIN()`** — the one-row-per-transaction-line
   guarantee from the 2026-10-15 fan-out fix. Duplicate `orderLine` entries break
   the `!transform/itemFulfillment` POST. The scoping adds a `WHERE`; it does not
   touch grouping.
2. **Every `LEFT JOIN` stays a `LEFT JOIN`.** The 2026-10-16 bin-guard fix relies
   on a non-bin location producing no `ibq`/`bin` row, so the bin id resolves to
   `0` by construction. Converting any of these to `INNER JOIN` would drop those
   lines entirely.
3. **No row filtering in SQL.** Per the 2026-10-16 partial-fulfillment plan and
   the 2026-10-20 backordered-tab fix, the same script feeds three consumers
   (mobile display, desktop STR grid, save guard). Closed, backordered and
   fully-packed lines must still come back; consumers filter. This change scopes
   the *inputs* to the aggregates, never the *output* rows.
4. **`tl.isclosed` and `tl.quantityshiprecv` stay projected** (closed-line guard).
   The outer `SELECT` list and outer joins were not touched.
5. **Output columns are unchanged** in name, type and order. Both DTO deserialization
   and the `TransferOrderLineVM` / `ReturnsLineVM` / `VendorReturnAuthorizationLineVM`
   computed properties consume them by name.
6. **No SQL comments** were added (Rule 7 — rationale lives here).

## Verification

- `dotnet test Integration.NS.Tests` — 478 tests, 477 passed. The single failure,
  `NetSuiteApiClientServiceTokenTests.HttpClient_has_explicit_timeout_of_100_seconds_by_default`,
  is the known static-`HttpClient` flake documented in
  `.opencode/docs/changes/2026-10-20-inventory-counting-mobile-fixes.md:139-143`;
  it passes in isolation and no `NETSUITE_HTTP_TIMEOUT_SECONDS` was set. It does
  not touch SQL.
- Targeted run of the script-shape suites (98 tests, all green):
  `NSScriptsWellFormedTests` (including its theory over every real `*.sql`),
  `SuiteQLTextCommentTests`, `SuiteQLDerivedTableCorrelationTests`,
  `PackingPendingFulfillmentFilterShapeTests`.

## Still required before deploy

1. **SuiteQL console check.** Run each script against a real tranid in NetSuite's
   SuiteQL console and confirm it parses and returns the **same row count and the
   same quantity values** as before. The shape is new to these three scripts even
   though it is Rule-1 compliant; `SuiteQL-Shape-Rules.md` ("Adding a new rule",
   steps 1–2) sets the process.
2. **Measure it.** Time the old and new script on one large order. This change is
   justified by the read of the query, not by a measurement — record the numbers
   so the next decision has a baseline.
3. **Mobile + web smoke test.** Open a packing order with bin-tracked lines, a
   non-bin location, a preferred bin, a vendor-assigned bin, and closed lines.
   The available-quantity columns and the bin resolution must look the same as
   before.

## Deferred (not in this change)

1. **Collapse `ibli1` + `ibli2` into one aggregate** using
   `SUM(CASE WHEN inventorystatus = '1' …)` / `'3'`, joined once. Halves the work
   again, but it is a new shape and needs console verification first.
2. **Paging.** These scripts are not paged at all: `GetTransferOrderLineQry.cs:24`
   passes no `limit`/`offset`, and `NetSuiteApiClientService.cs:895-898` only
   appends them when `limit > 0`. `hasMore` is declared but never read. Any order
   over ~1,000 lines silently loads a partial list. This is a correctness bug,
   tracked separately.
3. **Deduplicating the three near-identical scripts.** They have already drifted
   (only STR projects `isclosed` / `quantityshiprecv`). A shared script with a
   `category` parameter is a larger change with its own regression surface.

## Open business decisions

1. **Two different "available" numbers on one row.** The grid reads
   `LocationItemQuantityAvailable` from `AggregateItemLocation`, while the VM
   availability display reads `LocationItemQuantityAvailableGood/Bad` from summed
   `inventorybalance`. If they disagree, which one do pickers trust? If
   `AggregateItemLocation` totals only statuses 1 and 3, `ail` can be deleted
   outright — one fewer scan.
2. **Is the Good/Bad split needed on the packing screen?** If pickers only need
   the status they are picking, `ibli2` can go.
3. **Project `custbody_dbti_transfer_category` into the STR script.** Open item
   from `.github/specs/2026-10-16-returns-transfer-category-and-null-items-fix.md:186`.
   It must ship paired with the matching `PostTransferOrderIFCmd` guard; a
   half-shipped version hard-blocks STR packing.

## Rollback

Revert the three `.sql` files. No C# change is involved, no DTO or migration
changed, so a rollback is a straight file revert and needs only an app restart to
reload the scripts.
