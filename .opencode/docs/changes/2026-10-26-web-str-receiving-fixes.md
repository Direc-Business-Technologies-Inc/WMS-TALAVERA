# Web STR Receiving — fulfillment-scoped receipt quantities

Date: 2026-10-26  
Area: `Integration.NS/Implementations/Transactions/ReceivingIntegration.cs`,
`Application.DataTransferObjects/Transactions/Receiving/ItemFulfillmentLineRemainingDTO.cs`  
Related issues: 181, 182  
Status: WMS changes implemented; Issue 182 picker query revised after local NetSuite 500; sandbox validation remains required.

## Contract

For a Transfer Order line received against an Item Fulfillment, Web Receiving
uses one unit system and distinguishes order plan from this fulfillment's
shipped allocation:

```text
planned  = original quantity on the Transfer Order line
fulfilled = quantity shipped on this Item Fulfillment line
received = sum of Item Receipt line quantities natively linked to this Item Fulfillment
open     = min(planned, fulfilled) - received
```

Receipts against another Item Fulfillment for the same Transfer Order line do
not reduce this fulfillment's open quantity. The Transfer Order's
`quantityshiprecv` remains a separate order-wide safety limit.

## Changes in this repository

### Issue 181 — partial fulfillment receipt

- The IF line query takes `QuantityPlanned` from the original TO receiving line
  quantity and exposes the IF's shipped quantity separately as
  `QuantityFulfilled`. This prevents a remaining quantity from being displayed
  as the original plan, while capping receivable quantity to what this IF shipped.
- The UI now displays the fulfillment quantity for IF-based receiving.
- The IF → Transfer Order line derived table aggregates Item Receipt line
  quantities by source TO line, restricted to IRs that NetSuite natively links
  to the selected IF.
- The form therefore computes the open amount from fulfillment-scoped values.
- `PostItemReceipt` now re-reads the selected IF's line quantities immediately
  before posting. It rejects unmapped lines, ambiguous line mappings, and
  quantities above that IF's open quantity. The existing fresh TO-level check
  remains in place as a second ceiling across all fulfillments.
- If the fresh IF quantity read cannot be established, the post fails before
  writing to NetSuite.

For the reported example, once NetSuite attributes the six received units to
that fulfillment, the expected values are planned `9`, fulfilled `9`, received
`6`, open `3`; receiving `3` is allowed and receiving `4` is refused even if
the browser has stale values. For a partially shipped IF, `QuantityFulfilled`
limits the receipt amount even though the planned TO quantity is larger.

### Issue 182 — completed fulfillment picker

`GetSTRItemFulfillments` no longer trusts `custbody_dbti_fully_received`.
It compares each IF's total material quantity with material quantities from its
natively linked IRs and returns only IFs with a positive remainder. An IF with
no IR remains available, a partial IF remains available, and a fully received
IF is excluded even when the custom checkbox is stale.

The first implementation performed two unbounded aggregate derived-table scans
over `transactionline` and the incorrectly selected `nexttransactionlink` header
table inside the paged picker query.
NetSuite returned `500 UNEXPECTED_ERROR` for that shape even though the query was
syntactically valid. The picker now uses scoped stages:

1. Read the completed IF ids belonging to the selected STR (maximum 1,000; fail
   closed rather than return an incompletely verified list).
2. Aggregate fulfilled material quantities only for those IF ids.
3. Read native `(previousdoc, nextdoc)` IF-to-IR links only for those IF ids and
   deduplicate the pairs in C#. This uses the runtime-verified
   `nexttransactionlinelink` table. The similarly named `nexttransactionlink`
   header table returned `500 UNEXPECTED_ERROR` even for one candidate IF and
   without joins, so it must not be used for this relationship.
4. Aggregate receipt material quantities only for the linked IR ids and combine
   those totals with the deduplicated links in C#.
5. Apply grid filters, sorting, and paging to the resulting ids that still have
   a positive remainder.

This keeps Issue 182's quantity-derived source of truth and accurate grid counts
without asking NetSuite to aggregate account-wide transaction data. The existing
fresh server-side IF and TO quantity checks before posting remain unchanged.

## Confirmed NetSuite relationship

Runtime probes for IF `118077` and IR `123541` returned the same native link from
both `nexttransactionlinelink` and `previoustransactionlinelink`:

```text
previousdoc  = 118077
nextdoc      = 123541
previoustype = ItemShip
nexttype     = ItemRcpt
linktype     = TOrdCost
previousline = 0
nextline     = 1
```

The link is header-level (`previousline = 0`), so WMS uses it only to select the
IR documents belonging to an IF. Existing TO→IR line links provide the source
TO line and received quantity. This avoids item-name matching and requires no
new NetSuite field, historical backfill, or RESTlet 1853 modification.

## Deployment order

1. In a NetSuite sandbox, post partial receipts for two different IFs sharing a
   TO line. Verify each IF's received totals remain independent and the fully
   received IF disappears while the partial IF remains.
2. Run the WMS regression suite and deploy the WMS application/integration
   assemblies.
3. Acceptance-test Issue 181: IF plan 9, received 6, open 3; receive 3
   successfully; then attempt 1 more and verify rejection before a REST write.
4. Acceptance-test Issue 182: a partially received IF remains selectable; a
   fully received IF disappears after refreshing the picker; another pending IF
   under the same STR remains selectable.
5. Check NetSuite native Item Receipt links, WMS diagnostics, and the TO
   `quantityshiprecv` ceiling after each acceptance case.

Do not treat the WMS-only fake-client tests as proof of NetSuite SuiteQL or
RESTlet behavior. The account-specific sandbox matrix is a deployment gate.

## Automated coverage

- `ReceivingPostingTests` allows an exact partial remainder (`9 - 6 = 3`) and
  rejects an overage using the freshly queried IF state when the browser is
  stale.
- `GetItemReceiptItemFulfillmentLinesQueryShapeTests` verifies fulfillment-
  scoped receipt aggregation and that the select list does not add correlated
  subqueries.
- `GetSTRItemFulfillmentsQueryShapeTests` verifies candidate-scoped quantity
  reads, deduplicated IF-to-IR links, exclusion of fully received IFs, and final
  paging over pending ids. It also confirms the stale fully-received checkbox is
  no longer used.

Command:

```powershell
dotnet test "Integration.NS.Tests/Integration.NS.Tests.csproj" --no-restore --filter "FullyQualifiedName~ReceivingPostingTests|FullyQualifiedName~GetItemReceiptItemFulfillmentLinesQueryShapeTests|FullyQualifiedName~GetSTRItemFulfillmentsQueryShapeTests"
```
