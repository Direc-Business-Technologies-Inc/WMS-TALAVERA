# Transfer Order Item Fulfillment - Implementation Fix

Date: 2026-10-23  
Status: Superseded in part — see `.opencode/docs/TO-itemFulfillment-resolution-plan.md`  
Related RCA: `.opencode/docs/TO-itemFulfillment-RCA.md`

> **Read before implementing.** Two parts of this document are amended by the
> resolution plan: (1) fillers must carry an explicit `quantity: 0` and must
> **keep** their `inventoryDetail` at bin-tracked locations — the
> `itemreceive:false`-only shape in §"Target API contract" and Phase 2 is inert
> on an itemFulfillment and can produce `The total inventory detail quantity must
> be N`; dropping `inventoryDetail` from fillers re-triggers
> `INVALID_PARAMETER … 'InventoryDetail' … was not found in the request body`
> under `?replace=`. (2) Phases 1 and 2 are one deployable unit, and the
> revalidation query must be **paged** before reconciliation relies on it.
> Phase 6 (single-request GOOD/BAD) is deferred to its own ticket.

## Decision

Implement explicit line selection, but do not build the filler set from the
mobile display query.

Immediately before posting, the API must read the authoritative set of current
open transfer-order SHIPPING lines, then build the Item Fulfillment payload as:

- scanned/open lines: `itemreceive: true`, quantity, inventory detail;
- every other current open line: `itemreceive: false`;
- no closed or fully fulfilled line: never included;
- no scanned line absent from the current open-line set: reject before NetSuite.

This preserves the colleague's idea while fixing the more fundamental defect:
the mobile screen may intentionally load only one vendor, but NetSuite still
needs every open line explicitly addressed by the transform.

## Confirmed findings

### 1. The current payload is explicitly selective, but only for the loaded rows

`TransferOrderItemView.SaveScan` builds `TOItems` from the loaded Good/Bad
collections. The current vendor-gated mobile screen loads one vendor's slice,
not the complete transfer order. The API then receives only that slice, plus
zero-quantity rows created from the same slice.

`TransferOrderIFPayloadDTO.CreateForItemFulfillment` already emits:

```json
// unscanned row
{ "orderLine": 123, "itemreceive": false, "inventoryDetail": { "inventoryAssignment": { "items": [] } } }

// scanned row
{ "orderLine": 456, "itemreceive": true, "quantity": 3, "inventoryDetail": { "...": "..." } }
```

Therefore, simply changing the boolean logic is not sufficient. The missing
lines are those that were not loaded at all, especially other vendor lines or
lines removed by a display-query join.

### 2. The display query can omit lines that the transform still exposes

`NS_TO_x_Packing_Get_Items.sql` still uses an inner join to
`unitstypeuom`. A line with a null or unusable `tl.units` value is removed from
the WMS result even though NetSuite may still expose it on the Item Fulfillment
transform.

The query has also accumulated display-only joins for bins, vendors, UoM and
availability. These values are useful for scanning, but they are not a safe
authority for the set of lines that must be explicitly addressed during save.

The recent paging and aggregation changes fix truncation and row fan-out, but
they cannot restore a line removed by an inner join or intentionally excluded
by vendor scope.

### 3. The diagnostic item-name mismatch is not authoritative

`DecodeTOItemFulfillmentLine` tries to map NetSuite's display name to
`MaterialName` or `MaterialCode`. The RCA correctly notes that NetSuite display
names and WMS descriptions differ. Therefore:

- `not matched to a posted line` does not prove that the payload had no related
  `orderLine`;
- the exact identity must be determined by `orderLine` and the current TO line
  state, not by item text;
- the diagnostic should report set differences by line id and item internal id.

The CSV evidence is still significant: TO 771 had 63 distinct item lines while
the failing request reported only 4 payload entries; TO 776-2 had 1,657 lines
while the failing request reported 713 entries. The vendor-scoped read must be
considered when interpreting the second figure, but both cases justify a
save-time authoritative line reconciliation.

## Target API contract

The existing `POST /Packing/TransferOrder/SaveScan` remains the mobile save
endpoint. The server, not the handheld, owns the final line set.

Input rules:

1. The request must contain at least one positive scanned quantity.
2. All submitted rows must identify the same order.
3. Each submitted scanned row must identify one current open `orderLine`.
4. Duplicate `(orderLine, IsBad)` rows are rejected or deterministically
   collapsed with a logged conflict; they must never produce duplicate body
   entries.
5. A scanned line whose current remaining quantity is zero or whose line is
   closed fails with an actionable response.
6. A bin-tracked scanned line must have a valid vendor-assigned or preferred
   bin.
7. The server reads all current open lines and adds `itemreceive:false` entries
   for lines not included in the requested GOOD/BAD bucket.

The API must not silently report success when the request contains only zero
quantity rows.

## Implementation plan

### Phase 1 - Authoritative open-line query

Add `Integration.NS/NSScripts/NS_TO_Get_Open_Fulfillment_Lines.sql`.

The query should be deliberately small and free of display joins. It should
return at least:

- `transaction.id`;
- `transactionline.id` as `LineId` / `orderLine`;
- item internal id;
- absolute ordered quantity;
- current shipped/received quantity;
- `isclosed`.

Filter by transaction internal id, `transactionlinetype = 'SHIPPING'`, and the
same transfer-order record types used by the packing path. Define an open line
as `isclosed = 'F'` with positive remaining quantity. Use `ORDER BY tl.id` and
the existing metadata paging contract so a 1,000+ line document is complete.

Do not join UoM, inventory balance, bin assignment, vendor, or availability
tables in this query.

### Phase 2 - Reconcile immediately before posting

Update `NetSuiteApiClientService.SaveTOItemFulfillment`:

1. Validate that `Data` is non-empty and contains a positive scan.
2. Read the authoritative open-line set using the first line's
   `NetsuiteOrderInternalId`.
3. Reject scanned rows that are not in the current open-line set.
4. Reject scans exceeding current remaining quantity.
5. Treat a line that became fully shipped or closed after screen load as a
   conflict, not as a silently discarded filler.
6. Build the payload input from the authoritative open-line set:
   - merge scanned quantities and GOOD/BAD state for submitted rows;
   - create zero-quantity false rows for all other open lines;
   - carry bin/detail data only for scanned rows.
7. Run bin and quantity validation before the first BAD or GOOD POST.

The existing `GetFreshTOFulfillmentLineStateAsync` is a useful starting point,
but it currently uses a raw unpaged query and fails open when revalidation
fails. Replace it with the paged open-line query and fail closed: if current
line state cannot be established, do not post.

### Phase 3 - Make the display query lossless

Update `NS_TO_x_Packing_Get_Items.sql`:

- change `JOIN unitstypeuom` to `LEFT JOIN`;
- use a safe default conversion rate of `1` when no UoM row exists;
- use a safe base-unit label when `uom.unitname` is null;
- retain the current left-join behavior for inventory and bin data;
- keep the existing inventory-balance aggregation and stable `ORDER BY tl.id`;
- retain vendor scope for display efficiency;
- scope subqueries by transaction internal id where the caller can provide it;
- never filter a line out because availability, vendor-bin data, or UoM display
  data is missing.

A missing bin must be displayed as a data-quality problem and rejected only when
that line is actually scanned at a bin-tracked location.

### Phase 4 - Correct quantity and payload validation

All comparisons must use one clearly defined unit system. The current mobile
scan quantity is expressed in the line's display UoM, while the raw SQL
quantities are stock quantities. Normalize before comparing:

```text
remainingDisplayQty = (orderedStockQty - shippedStockQty) / UoMRate
```

For each scanned line, validate:

```text
scannedDisplayQty > 0
scannedDisplayQty <= remainingDisplayQty
sum(inventoryAssignment.quantity) == fulfillment quantity
```

Use a decimal tolerance appropriate for the account's UoM precision. Reject a
zero or invalid UoM rate before any NetSuite call.

The mobile over-scan guard must allow an exact final scan; a guard using `>=`
before adding the next scan should use `>` unless the incoming scan itself is
known to be greater than one unit.

### Phase 5 - Improve diagnostics

Replace item-name-only diagnosis with a structured save diagnostic containing:

- order internal id and order number;
- current open-line count;
- submitted-line count;
- payload-line count;
- duplicate order lines;
- scanned lines missing from current open lines;
- current open lines missing from the payload;
- WMS line id, item internal id, item code, quantity, UoM rate, and bin for the
  failing scanned line.

Keep the NetSuite error text, but treat item-name matching as display-only
fallback information.

### Phase 6 - Prevent the GOOD/BAD partial-save hazard

The current flow posts BAD and GOOD in separate requests. If BAD succeeds and
GOOD fails, a retry can duplicate BAD inventory.

Preferred fix: submit one Item Fulfillment containing both GOOD and BAD scanned
lines, with the inventory status resolved per line. If NetSuite or business
rules require two posts, then after the first success re-read current quantities
and build the second request from the remaining unfulfilled scans. Never retry
the first bucket blindly.

## Files expected to change

### Required

- `Integration.NS/NSScripts/NS_TO_Get_Open_Fulfillment_Lines.sql` - new
  authoritative open-line query.
- `Integration.NS/NSScripts/NS_TO_x_Packing_Get_Items.sql` - lossless display
  joins and safe UoM defaults.
- `Integration.NS/Services/NetSuiteApiClientService.cs` - save-time
  reconciliation, fail-closed revalidation, quantity/bin guards, and exact
  diagnostics.
- `Application.DataTransferObjects/Transactions/Commons/NS/PostTransferOrderDTO.cs`
  - explicit quantity/UoM or vendor fields if needed by reconciliation.
- `Mobile.MAUI/Components/Pages/Packing/TransferOrder/TransferOrderItemView.razor.cs`
  - exact final-scan guard and clear save-state behavior.

### Tests

- `Integration.NS.Tests/Payloads/PackingIFPayloadDTOTests.cs`
- `Integration.NS.Tests/Services/NetSuiteApiClientServiceTOFulfillmentDiagnosticsTests.cs`
- new open-line reconciliation tests under
  `Integration.NS.Tests/Implementations/Transactions/Packing/` or
  `Integration.NS.Tests/Services/`
- paging tests for the authoritative open-line query
- mobile tests for exact final scan, UoM conversion, and vendor-scoped save

If the RESTlet option is selected, add the NetSuite SuiteScript separately; it
is outside this repository and must be versioned/deployed with its own tests.

## Acceptance criteria

### Automated

- A 1,657-line order produces a complete authoritative open-line set.
- No duplicate `orderLine` is sent in a bucket.
- A vendor-scoped mobile read can save without omitting other vendors' open
  lines from the false-selection set.
- A missing display UoM row does not remove the line.
- A scanned line missing from the current TO is rejected before HTTP POST.
- A stale/closed scanned line is rejected before HTTP POST.
- A missing bin is rejected before HTTP POST.
- Assignment quantity mismatch is rejected before HTTP POST.
- An all-zero request is rejected before HTTP POST.
- A failed revalidation query prevents posting.
- GOOD and BAD retries cannot duplicate a previously successful bucket.

### Live NetSuite / device

Run in a sandbox or controlled account before production:

1. Open bin-tracked TO with one scanned line and several unscanned lines.
2. Multi-vendor TO where the handheld loads only vendor A but vendor B has open
   lines.
3. TO with a line beyond row 1,000.
4. TO with a missing `tl.units`/UoM record.
5. TO with a missing preferred/vendor bin.
6. Partial fulfillment changed after the handheld loaded the screen.
7. Mixed GOOD/BAD scan on the same order.
8. UoM rates greater than 1, less than 1, and fractional quantities.
9. Retry after simulated GOOD or BAD bucket failure.

Capture the exact request JSON and NetSuite response for each case. The local
tests prove payload construction and guards; only the live/device checks prove
NetSuite transform semantics.

## Deployment order

1. Deploy API and SQL changes with save-time reconciliation and fail-closed
   guards.
2. Run the controlled NetSuite smoke tests.
3. Deploy the mobile APK containing the corrected over-scan and save behavior.
4. Validate one multi-vendor order and one large order on a real device.
5. Monitor diagnostics for missing/extra line sets and duplicate fulfillment
   attempts.

## Fallback: NetSuite RESTlet

If the REST transform continues to reject explicit false fillers or behaves
differently across accounts, move the final construction into a NetSuite
RESTlet. The RESTlet should transform the TO, iterate the actual transformed
`item` sublist, read each line's `orderline`, set `itemreceive:false` for
unscanned lines, apply quantity and inventory detail only to scanned lines, set
`shipstatus`, and save once.

That is the strongest long-term design because NetSuite itself determines the
valid transformed line set. It requires SuiteScript deployment and is not a
replacement for the immediate server-side guards.

## Explicit non-goals

- Do not rely on NetSuite item display names to identify lines.
- Do not use the mobile vendor-filtered result as the complete fulfillment set.
- Do not silently ignore lines missing from the current open-line query.
- Do not continue posting when save-time revalidation fails.
- Do not claim the workflow is fully fixed until the live NetSuite/device
  acceptance matrix passes.
