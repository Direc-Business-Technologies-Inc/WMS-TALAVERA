# Fix: TO packing excludes fully backordered fulfillment fillers

Date: 2026-11-10  
Status: Implemented; local regression tests passed; target-account validation pending  
Scope: Shared backend transfer-order packing save, used by mobile and web; REST transform and RESTlet preflight

## Summary

Transfer-order packing could send a filler for a fully backordered line that
NetSuite did not offer on its native Item Fulfillment form. The backend treated
an outstanding source line as fulfillment-eligible even when all of its remaining
quantity was backordered. Addressing a nonexistent transformed line is consistent
with NetSuite's `USER_ERROR` invalid static-sublist operation.

The correction excludes such lines from reconciliation and payload preparation,
while preserving explicit deselection of other eligible, unscanned lines. A
positive scan on an ineligible line is rejected rather than silently discarded.

## Investigation evidence

Reported order: `STR-TGC00000574`, internal ID `302986`.

The failed GOOD save reported `usedBin=True`, `replace=True`, `open=3`,
`submitted=5`, `posted=3`, `scannedLines=1`, `missing=2`, and
`postedIds=[5,2,8]`. The screenshot build began `e8287b8e`. The next commit,
`8b550ef4`, changed PO receiving filters, not TO fulfillment eligibility.

SuiteQL and source-order screenshots established:

| Item | SHIPPING ID | Raw ordered | Raw shipped | Raw backordered | UoM rate | Source display quantity |
| --- | --- | --- | --- | --- | --- | --- |
| TEST-001 | 2 | 2.04 | 0 | 2.04 | 12 | 0.17 BOX12 |
| TEST-002 | 5 | 11 | 0 | 0 | 1 | 11 PC |
| TEST-003 | 8 | 5 | 0 | 0 | 10 | 0.5 BOX10 |

All three SHIPPING lines were not closed and had zero packed quantity. The
related Item Fulfillment lookup returned no rows. Source `id` and
`linesequencenumber` matched for this order.

Opening the native, unsaved Fulfill form exposed only TEST-002 and TEST-003.
Both were selected by default at their remaining quantities. TEST-001 was absent.
This establishes a mismatch between the former SQL-derived set and native
fulfillment eligibility, and explains why retaining the TEST-003 deselection
filler is essential.

Evidence limits: the native form is not a capture of the REST transform. No
client-server payload logs or live corrected REST posting were available during
implementation. This is a strongly supported cause of the reported failure, not
proof that all invalid-sublist errors have this cause. A recent regression was
not established.

## Root cause in the implementation

`Integration.NS/Services/NetSuiteApiClientService.cs` previously classified fresh
lines through `TOFulfillmentFreshLineState.IsOpen` using only:

```text
not closed AND ordered - shipped > 0
```

`ReconcileTOFulfillmentLines` generated fillers for every member of that set.
The set therefore included fully backordered line 2. Its zero scan produced an
`itemreceive:false` entry with empty inventory assignments at the bin-tracked
location. Deselection does not make an absent transformed line addressable.

The `extra=0` diagnostic compared IDs against that same SQL-derived set; it could
not detect this eligibility error. The existing `missing` label counts generated
fillers, not lines still missing from the posted payload.

## Implemented behavior

A shared TO-specific eligibility predicate now requires:

```text
not closed AND ordered - shipped - backordered > 0
```

All quantities in this predicate are raw stock units from the existing fresh
SuiteQL query. UoM conversion is unnecessary for the eligibility sign test.

- `TOFulfillmentFreshLineState.IsOpen` uses the predicate, so reconciliation,
  authoritative open-ID diagnostics, and completeness expectations agree.
- `PrepareTOFulfillmentEligibleLines` applies the same predicate before payload
  preparation in both REST and RESTlet saves. This also covers reconciliation
  disabled and existing fail-open snapshot fallback behavior.
- Positive scans against ineligible lines fail before any fulfillment write,
  identifying the affected material/line. Reconciliation errors now explicitly
  include fully backordered lines as an eligibility conflict.
- Zero-scan ineligible rows are omitted.
- Eligible unscanned rows remain fillers; eligible positive scans retain the
  existing quantity/bin validation.
- Partially backordered lines remain eligible if non-backordered unshipped
  quantity exists. Existing scan ceilings continue to limit their quantity.
- Packed quantity is deliberately not subtracted for sublist eligibility:
  packed but unshipped stock may still be fulfillable. The separate scan ceiling
  continues to account for packed quantity.

For the reported GOOD scan, the expected payload IDs become `[5,8]`. Line 5
carries the actual scan; line 8 remains explicitly unselected; line 2 has no
item or inventory-detail operation.

This patch does not alter line identity mapping, payload field casing, inventory
assignment serialization, replacement URLs, UoM configuration, or NetSuite
commitment data. It requires no database migration, SuiteScript deployment, or
new environment flag. The fix is in the backend; deploy the API hosting this
integration for mobile requests to use it.

### Fallback limits

When fresh reads fail and `WMS_TO_IF_ENABLE_FAIL_CLOSED_REVALIDATION` is false,
the existing fail-open behavior still relies on client quantities. The new
predicate filters that snapshot, but cannot recover current commitment changes
that the snapshot does not contain. With fail-closed enabled, an unreadable
fresh state continues to block the save.

`WMS_TO_IF_ENABLE_RECONCILIATION=false` does not bypass eligibility validation;
it only disables authoritative filler completion. Full rollback of this fix
requires reverting the code change. Unsupported GET transform preview remains
disabled by default.

## Regression coverage

`Integration.NS.Tests/Services/NetSuiteApiClientServiceTOFulfillmentReconciliationTests.cs`:

- Reproduces the reported raw quantities and mixed UoMs.
- Omits fully backordered line 2, retains scan 5 and filler 8.
- Checks the eligible-set completeness guard.
- Rejects positive scans on fully backordered lines.
- Covers partially backordered, partially shipped, and packed-but-unshipped
  eligibility; fresh-state and snapshot predicates agree.

`Integration.NS.Tests/Services/FulfillmentSaveStabilityTests.cs`:

- Captures the actual serialized outbound body at the in-memory HTTP boundary.
- Asserts IDs `[5,8]`, positive scan quantity, explicit filler deselection, and
  omission of filler quantity for GOOD and BAD saves, with reconciliation on/off.
- Verifies stale positive scans on freshly backordered lines cause zero writes
  for REST and RESTlet saves, with reconciliation on/off.
- Verifies failed-fresh-read snapshot fallback omits zero-scan backordered lines
  and rejects positive scans instead of silently dropping them.

Executed:

```powershell
dotnet test "Integration.NS.Tests" --no-restore --filter "FullyQualifiedName~Fulfillment|FullyQualifiedName~PackingIFPayload" --verbosity minimal -p:WarningLevel=0
```

Result: **189 passed, 0 failed, 0 skipped**. These tests exercise local code and
an in-memory HTTP boundary, not live NetSuite acceptance. The initial build also
reported existing repository compiler/analyzer warnings.

## Target-account acceptance procedure

1. Deploy the corrected API to the controlled test environment.
2. Use an equivalent order with one fully backordered line and two eligible
   lines; verify native Fulfill excludes the fully backordered item.
3. Reload the mobile order, scan only one eligible item, and save.
4. Verify only the intended scanned quantity was fulfilled, with correct bin and
   status. Verify the other eligible item was not auto-fulfilled and the
   backordered item remains outstanding.
5. Repeat with a partial backorder, a prior partial shipment, and a mixed
   GOOD/BAD scan. Verify a newly fully backordered scanned line blocks all writes.
6. Confirm a second partial fulfillment still addresses the current eligible
   set. If the invalid-sublist error remains, investigate actual REST line
   identity and inventory assignments rather than changing master data or
   deleting all fillers.

## Follow-up diagnostic query

This reproduces the corrected classification without changing data:

```sql
SELECT
    tl.id AS line_id,
    BUILTIN.DF(tl.item) AS item_name,
    ABS(tl.quantity) AS ordered_quantity,
    NVL(tl.quantityshiprecv, 0) AS shipped_quantity,
    NVL(tl.quantitybackordered, 0) AS backordered_quantity,
    tl.isclosed,
    CASE
        WHEN NVL(tl.isclosed, 'F') <> 'T'
            AND ABS(tl.quantity)
                - NVL(tl.quantityshiprecv, 0)
                - NVL(tl.quantitybackordered, 0) > 0
        THEN 'ELIGIBLE_BY_WMS_RULE'
        ELSE 'EXCLUDED_BY_WMS_RULE'
    END AS eligibility
FROM transactionline tl
WHERE tl.transaction = 302986
    AND tl.transactionlinetype = 'SHIPPING'
    AND tl.item IS NOT NULL
ORDER BY tl.id
```

The classification remains a SuiteQL approximation, not a universal guarantee
of transform eligibility under every account-specific fulfillment rule.
