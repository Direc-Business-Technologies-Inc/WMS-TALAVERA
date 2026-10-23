# Web receiving: false success and partial NetSuite receipts

Date: 2026-10-23

## Problem

An operator could submit receiving on the web and see a success notification while
the NetSuite source order remained Pending Receipt or Pending Receipt/Partially
Fulfilled. Code review identified a reproducible false-success path; identifying
which path affected a particular production order still requires that order's
receipts and application logs.

### Root causes

1. **No request could be reported as success.** `PostItemReceipt` created requests
   only for inventory details carrying status 1 (GOOD), 3 (BAD/CONFISCATED), or 6
   (MISSING). With no matching allocations, `Task.WhenAll` on an empty collection
   completed normally and the method returned `true`.
2. **Non-bin items bypassed allocation checks.** `IsAllAssigned` returned true for
   those items, and their inventory-details button was hidden. Entering a quantity
   alone did not produce the status details required by the posting code.
3. **Independent requests could partially commit.** GOOD, BAD, and MISSING writes
   started concurrently. One successful request was not rolled back when another
   failed. A retry could re-submit already-received quantities.
4. **Missing fulfillment references used a sentinel.** The page converted a missing
   Item Fulfillment ID into `-1` rather than rejecting the transfer receipt.
5. **Submission did not distinguish receipt creation from order completion.** A
   successful partial receipt does not necessarily complete its source order.

## Implemented behavior

### Preflight validation

`Application.DataTransferObjects/Transactions/Receiving/ItemReceiptValidation.cs`
defines the web receiving submission contract. The NetSuite integration invokes it
before any query or write:

- A positive source-order internal ID is required.
- Transfer-order and Returns receipts require a positive Item Fulfillment ID.
  Purchase-order receipts do not require one.
- At least one checked line must have a positive quantity.
- Checked allocations must be nonnegative and sum exactly to the entered quantity.
- Every positive allocation must have status 1, 3, or 6.
- Quantities must fit the open quantity in the loaded document; the existing fresh
  transfer-order ledger check also runs before writes.
- Selected positive lines require valid, unique NetSuite line numbers.
- PO allocations for bin-controlled items at bin-controlled locations require bins.

Unchecked lines cannot create receipt buckets. Zero allocations cannot create
requests. Non-bin items require explicit status allocations just like bin items.
No default GOOD status is inferred from an entered quantity.

### Web form

- Inventory details are available for every item, including non-bin items.
- The dialog offers the supported receiving statuses and hides bin selection for
  non-bin items even when their warehouse uses bins.
- Allocation equality and supported statuses are checked for every selected line.
- Pending barcode scans are applied before quantity/allocation checks.
- A missing fulfillment ID stays null and is rejected by server validation.
- A false posting result is treated as a failure.
- The success notification says the receipt was posted and explains that the source
  order may still be open. It does not claim full order completion.

### Posting

`Integration.NS/Implementations/Transactions/ReceivingIntegration.cs`:

1. Validates the submission and performs the existing transfer remainder check.
2. Builds all payloads before the first write.
3. Rejects a plan with zero requests.
4. Posts sequentially in the existing category order: BAD/CONFISCATED, GOOD, MISSING.
5. Stops on the first failure and preserves its original exception as the cause.
6. Reports categories whose requests already succeeded and explains how to reconcile
   the receipt before retrying. Later categories are not sent.
7. Returns success only after every planned write returns successfully.

Payload logging now occurs immediately before each attempted write, so a later
unattempted category does not appear as a posted request in the logs.

For purchase orders, unchecked open lines retain explicit `itemreceive=false`
fillers. Omitting them could allow transform defaults to receive unintended lines.
The planner excludes lines exhausted by an earlier category from later payloads,
avoiding references to lines no longer present on the transform's static sublist.
Bin-tracked PO transforms use
`?replace=item.inventoryDetail.inventoryAssignment` to preserve supplied assignments.

The command handler no longer turns an exception with the text
`Empty response from NetSuite API` into success. Actual empty HTTP-success bodies
remain accepted by the existing HTTP client.

## What the fix guarantees and its boundaries

The fix prevents zero-request success and makes split-request failures explicit.
It does not introduce a distributed transaction: a successful NetSuite write cannot
be rolled back by this application when a later category fails. A timeout can also
occur after NetSuite commits, so even the failing category must be checked before
retrying. There is no automatic retry or persisted idempotency record added here.

The HTTP client's existing success/deserialization contract remains in place. The
RESTlet implementation (script 1853/deployment 1) is external to this repository;
its internal behavior and any application-level errors returned with HTTP 2xx
still require sandbox validation. No post-submit query asserts that the entire
source order is complete, and no source-order status is forced closed.

The separate mobile `SavePOItemReceipt` workflow has not been consolidated with the
web workflow. Their payload models differ. This fix adds the relevant web guards
without changing the mobile contract.

## Operator procedure

1. Select the source order and, for transfers/Returns, its shipped fulfillment.
2. Check the lines to receive and enter their quantities.
3. Open inventory details for each positive line, including non-bin items. Allocate
   the full quantity to GOOD, BAD/CONFISCATED, and/or MISSING. Select bins when required.
4. Add remarks and submit.
5. On success, verify outstanding quantities if the source order remains open. A
   partial receipt can legitimately leave a pending status.

### Recovering a partial or uncertain post

1. Record the source order, selected fulfillment, timestamp, and complete error text.
2. In NetSuite, inspect related Item Receipts and WMS receiving categories and
   reconcile their quantities against the shipped fulfillment/source order.
3. Use application logs containing `PostItemReceipt payload` and
   `confirmed statuses` to identify attempted categories and successful responses.
4. Check the failed category too if the error was a timeout or connection failure.
5. Reload the source and allocate only the actual remaining quantities. Do not
   re-submit the original mixed-status receipt unchanged.

## Automated regression coverage

`Integration.NS.Tests/Implementations/Transactions/ReceivingPostingTests.cs` exercises
the actual receiving integration using the repository's capturing NetSuite fake:

- Empty/zero/unchecked-only receipts fail without NetSuite calls.
- Missing/unsupported statuses, incomplete totals, negative quantities, duplicate
  lines, missing IDs, and over-remaining quantities fail before posting.
- Non-bin receipts with explicit status allocations post successfully.
- Missing PO bins are rejected; bin-controlled posts use the replacement URL.
- Unchecked PO fillers remain false and do not create extra status buckets.
- Later PO buckets omit lines fully received by earlier buckets.
- A delayed response prevents the next category from starting.
- Failure stops later categories and preserves the original cause and recovery text.
- Transfer payloads omit unchecked lines and zero allocations.
- Command errors cannot be converted to success by exception-message matching.

Commands:

```powershell
dotnet test "Integration.NS.Tests/Integration.NS.Tests.csproj" --no-restore
dotnet build "Web.BlazorServer/Web.BlazorServer.csproj" --no-restore
```

Verification on 2026-10-23:

- All 27 new receiving regression cases pass in the full integration test run.
- Full integration suite: 644 passed, 1 failed (645 total). The pre-existing failure
  is `NetSuiteApiClientServiceTokenTests.HttpClient_has_explicit_timeout_of_100_seconds_by_default`:
  the test expects 100 seconds while the configured client uses 30 seconds. It also
  failed before this change.
- Web build succeeds with 0 errors; the project emits existing compiler/analyzer warnings.
- No live NetSuite transactions or browser acceptance tests were run.
- The AST knowledge graph was refreshed with `graphify update .`; its existing SQL
  extractor dependency warning (`tree_sitter_sql` missing) is separate from this fix.

## Sandbox acceptance checks

Automated tests use an in-process fake and do not write to NetSuite. Before rollout,
verify these flows in the configured sandbox:

| Scenario | Expected result |
| --- | --- |
| Positive non-bin quantity without details | Validation error; no receipt |
| Same line with complete GOOD allocation | One receipt with expected quantity |
| Transfer receipt with missing fulfillment ID | Validation error before posting |
| Mixed GOOD/BAD/MISSING receipt | Sequential receipts with correct categories and quantities |
| BAD succeeds, GOOD is rejected | Partial-post error; MISSING is not attempted |
| PO has checked and unchecked open lines | Only checked positive quantities received |
| First PO bucket fully receives a line | Later payloads omit that exhausted line |
| Partial shipment/receipt | Source status remains pending until remaining work is complete |
| Bin-controlled PO | Inventory assignments use the selected bins |

Deploy the updated web application and its dependent assemblies together. No schema
migration is required.
