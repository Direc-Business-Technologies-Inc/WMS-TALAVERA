# Receiving: operational fulfillment identity instead of accounting line IDs

Date: 2026-10-27

## Confirmed evidence and root cause

Live NetSuite evidence supplied for **IF230512**:

| Surface | Identity / values |
| --- | --- |
| Operational item sublist | `line=0`, `orderdoc=208281`, `orderline=299`, item `49312`, quantity `3`, units `1` (PC), unitconversion `1` |
| Source TO SHIPPING | line `299`, quantity `-10`, units `1`, `transferorderitemlineid=298` |
| Source TO RECEIVING | line `300`, quantity `10`, units `1`, `transferorderitemlineid=298` |
| Native previous-transaction line link | TO previousline `299` → IF nextline `0`, linktype `ShipRcpt` |
| SuiteQL IF accounting rows | id `1`, sequence `1`, INTRANSIT quantity `+3`; id `2`, sequence `2`, blank type quantity `-3`; **both have null units** |

The old `IF_TO_LINE_MAP` equated native operational `nextline` to
`transactionline.id`. That cannot identify operational line zero in this example.
Both the UI load and the fresh posting check also inner-joined UoM using the IF
accounting row's null units, dropping the rows. Summing the absolute accounting
quantities would count this shipment twice. Line sequence, ID offsets, item-only
matching, and substituting conversion rate 1 do not repair that identity error.

The earlier receipt aggregation joined receipt-line links to IF receipt links on
receipt document only; multiple native links could multiply a quantity. Deduplication
must use record/line identity, never `SUM(DISTINCT quantity)` (two legitimate receipts
of quantity 1 must count as 2).

## Implementation

`Integration.NS/Implementations/Transactions/ReceivingIntegration.OperationalLines.cs`
contains the shared operational read and validation path. It is used by:

- `GetItemReceiptItemFulfillmentLinesAsync`: resolve the exact IF internal ID from
  tranid and record type, then build receipt rows from verified operational lines.
- Fresh non-PO posting verification: re-read the selected IF and verify its source
  is the submitted source order before accepting its line quantities.
- `GetSTRItemFulfillments`: evaluate each candidate's operational line remainder
  before paging pending fulfillments. It no longer sums IF accounting quantities
  or whole-receipt accounting quantities.

The mapping is an explicit identity chain:

1. GET the IF record through the existing `INetSuiteApiClientService.MakeRequest`
   transport with `?expandSubResources=true`.
2. Match the operational record's source, `orderLine`, and `line` against native
   `TrnfrOrd → ItemShip / ShipRcpt` links.
3. Match SHIPPING to RECEIVING within the same source document using
   `transferorderitemlineid`, line types, and item identity. Require exactly one
   receiving identity and matching operational item/unit IDs. Thus 299 → 300 is
   derived from shared transfer line 298, not arithmetic.
4. Load destination location, item metadata, planned quantity, UoM, barcode, and
   preferred bin from the TO RECEIVING row using the existing lookup-collapse
   protections. The outward `ItemReceiptLineDTO.LineNumber` remains the TO
   RECEIVING line ID expected by the posting contract.
5. Use the operational quantity directly in its verified units. No IF accounting
   quantity or IF accounting UoM is consulted; no second conversion is performed.

For receipts, enumerate distinct IF→IR `TOrdCost` document identities, verify each
receipt belongs to only the selected IF, and GET each operational receipt once.
Every receipt item must match its TO `ShipRcpt` native operational line link and
the verified receiving line's source/item/units. Sum each operational receipt item
once into that receiving line. Repeated items remain separate by source line.
Receipts spanning multiple IF owners are rejected rather than apportioned by item.

The existing fresh TO `quantityshiprecv` ledger ceiling still runs after the IF
ceiling. It catches TO-linked mobile receipts that may not have an IF attribution.
An absent ledger line now fails closed rather than skipping that ceiling.

## REST contract: assumptions requiring sandbox verification

The repository already uses REST `item.items`, camel-case `orderLine`, reference
objects, and `JsonElement` for uncertain record data. The shared transport already
deserializes System.Text.Json. No new transport or RESTlet is introduced.

**The supplied live evidence confirms operational SuiteScript/sublist semantics;
it is not a captured expanded REST response.** The following expanded REST contract
is enforced by synthetic contract/regression fixtures, but still needs confirmation
against this account's record service and integration role:

- Record `id` and `createdFrom.id`; expanded `item.items` array.
- Per item: `line` (integer, zero allowed), `orderLine` (positive source line),
  `item.id`, `units.id`, and positive `quantity` in the referenced units.
- An optional `orderDoc`, when present, must equal `createdFrom.id`. The root source
  plus the native previous-document link provides source identity when it is absent.
- Numeric JSON values and numeric strings are accepted for IDs/quantities; missing,
  fractional IDs, invalid reference objects, null units, and invalid quantities fail.
- IF native `nextline` must identify REST operational `line`. Receipt `orderLine`
  must identify the TO RECEIVING line, with the corresponding native TO→IR
  `ShipRcpt` link. These receipt REST/link details are assumptions pending live checks.

There is no fallback to accounting rows, guessed offsets, item-only matching, or
default units. If this account exposes a different contract, the operation blocks
with a verification error until an evidenced adapter/contract is supplied.

## Completeness and ambiguity behavior

- SuiteQL reads use a maximum 1,000-row page and reject `hasMore`, nonzero offset,
  a next-page link, or a total exceeding the returned row count. This change rejects
  incomplete reads rather than implementing automatic pagination.
- Expanded REST sublists reject supplied pagination metadata describing a partial
  page (`hasMore`, offset, count/total mismatch, next link). Expanded lists may omit
  pagination metadata, so their complete identity set must also match the native
  line-link set. Missing operational line fields never become an implicit zero.
- Duplicate operational line IDs, ambiguous receiving mappings, multiple IF lines
  targeting one receiving line, missing source metadata, and shared receipt owners
  fail before a write. Distinct native receipt-document links prevent fanout.
- Preferred-bin and barcode lookup aggregation continues to prevent metadata
  joins from multiplying source rows.

## Prior protections retained

See `../web-receiving-netsuite-status-fix.md` and repository history including
`67b58182` (IF-scoped quantities), `5de6fa71` (native IF→IR line links), and
`5b94ba50` (receipt lookup fanout). This corrects the operational/accounting identity
assumption while retaining:

- Selected-positive-line uniqueness and allocation/status validation.
- IF shipped-minus-received ceiling and separate fresh TO ledger ceiling.
- PO unchecked-line `itemreceive=false` fillers, later-bucket exhaustion handling,
  bin assignment replacement, and sequential BAD/GOOD/MISSING posting.
- Stop-on-failure/partial-post recovery messaging and existing payload contracts.
- Mobile PO explicit source-line selection; no mobile posting code is changed.
- The earlier unsupported-correlated-SuiteQL fix: the new mapping queries are
  ordinary scoped joins; no nested correlated scalar subqueries are introduced.

## Verification performed

- Targeted integration tests: **87 passed, 0 failed**:

  ```powershell
  dotnet test "Integration.NS.Tests/Integration.NS.Tests.csproj" --no-restore --filter "FullyQualifiedName~Receiving|FullyQualifiedName~GetSTRItemFulfillments|FullyQualifiedName~PurchaseOrderIRPayload|FullyQualifiedName~ItemReceipt" --verbosity quiet
  ```

- Root `dotnet build --no-restore --verbosity quiet`: **0 errors, 518 warnings**.
  The initial build exceeded a 120-second tool timeout; the subsequent invocation
  completed in 40.84 seconds. Existing compiler/analyzer warnings remain.
- Regression fixtures cover zero operational identity versus accounting IDs 1/2
  with null units, 299→300 mapping, repeated items, missing fields, incomplete REST
  and SuiteQL reads, ambiguous native links, distinct equal-quantity receipts,
  duplicate receipt links, shared receipt owners, non-base conversion handling,
  missing TO ledger lines, both quantity ceilings, and pending-picker behavior.
- `git diff --check` completed without whitespace errors.
- `graphify update .` completed: 19,866 nodes / 46,832 edges. The existing
  missing `tree_sitter_sql` dependency warning remains; SQL extraction was skipped.

## Pending live sandbox acceptance and operational limits

1. With the integration role, GET expanded IF230512 and confirm the field contract
   above; require one item with quantity 3 PC mapped to receiving line 300.
2. Confirm expanded IR fields and native TO→IR `ShipRcpt`/IF→IR `TOrdCost`
   attribution using partial receipts, repeated-item lines, and two receipts of
   equal quantity. Confirm no unlinked/voided receipt is incorrectly included.
3. Verify a receipt of 3 succeeds when both ceilings allow it, 4 is rejected before
   POST, and concurrent mobile receipts reduce the TO ledger ceiling.
4. Verify the picker removes an IF after its operational quantities are received;
   exercise non-base units and large/incomplete sublists.

No live posting or REST contract verification was performed during this change.
The picker now makes per-candidate operational reads, plus per-receipt reads and
identity queries; assess latency on large orders. It intentionally blocks the
picker if any candidate cannot be verified safely. Fresh reads are not an atomic
NetSuite reservation: another actor can receive between validation and posting.
Independent status receipts still require reconciliation after partial/uncertain
responses; no distributed transaction or idempotency mechanism is added.
