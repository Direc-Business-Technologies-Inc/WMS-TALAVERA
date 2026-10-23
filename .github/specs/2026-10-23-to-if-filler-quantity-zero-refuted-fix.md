# Fix: TO Item Fulfillment — `quantity: 0` filler refuted in the field ("Value 0.0 outside of valid min/max range for field quantity")

Date: 2026-10-23
Mode: Bugfix / RCA / Follow-up to [2026-10-23 TO IF reconciliation change](../../.opencode/docs/changes/2026-10-23-to-if-reconciliation.md)
Area: `Application.DataTransferObjects/Transactions/Packing/NS/Payload/*IFPayloadDTO.cs`, `Integration.NS.Tests/Payloads/PackingIFPayloadDTOTests.cs`

## Related

- [2026-10-23 TO IF reconciliation change](../../.opencode/docs/changes/2026-10-23-to-if-reconciliation.md) — introduced Phase 2a (`quantity: 0` fillers); its open question **U2 is answered here**
- [2026-10-15 IF `?replace=` / INVALID_PARAMETER fix](2026-10-15-if-replace-param-invalid-parameter-fix.md) — established the empty-assignment filler that **passed in the field** (Transaction A)
- [TO itemFulfillment resolution plan](../../.opencode/docs/TO-itemFulfillment-resolution-plan.md) — the variant matrix (0.3) and the U1 probe (0.2)

## Key Takeaway

The `itemFulfillment` transform **rejects a line quantity of 0**: `Quantity = 0`
on a zero-quantity filler line fails the whole save with
`[USER_ERROR] Value 0.0 outside of valid min/max range for field quantity`.
The fillers reverted to carrying **no `quantity` property** — the exact shape
that passed in production on 2026-10-15 (Transaction A). Serialization snapshot
tests now pin those bytes so the wire shape cannot drift again silently.

## Field evidence

Order `STR-THM00100000881/226006`, build `1.0.0+ce8da8a7` (the same-day commit
that introduced `Quantity = 0`):

```
[USER_ERROR] Error while accessing a resource. Value 0.0 outside of valid
min/max range for field quantity.
[diag bucket=GOOD replace=True usedBin=True order=STR-THM00100000881/226006
open=1022 submitted=57 posted=1022 scannedLines=6 missing=1016 extra=0 dup=0
uomRate=72 ... build=1.0.0+ce8da8a7...]
```

Reading the counters: the GOOD bucket posted **1,022 entries = 6 scanned lines +
1,016 zero-quantity fillers**. Exactly on that day, Phase 2a had changed every
filler from `{orderLine, itemreceive:false, inventoryDetail:empty}` (no
quantity) to also carry `quantity: 0`. NetSuite rejects a transform line
quantity below its valid range, and 0 is out of range — so every filler was an
offender and the entire GOOD post failed. `extra=0 dup=0` proves the line SET
was correct; only the filler's quantity value was.

## Why variant 2 was shipped anyway

The 2026-10-23 reconciliation change selected variant 2 ("`quantity: 0` +
empty assignment") as the preferred fix for the earlier
`The total inventory detail quantity must be N` error. The sandbox matrix
(U2) that the plan required before deploy was never run; the "must be N" cause
itself was also never verified (U1: filler identity vs UoM rate). The field
answered U2 first — variant 2 is dead.

The U1 suspicion has sharpened: the field diag carries `uomRate=72`, and
"must be 3" is consistent with an assignment-sum vs line-quantity **unit
mismatch** on a scanned line, not a filler problem. **Do not re-attempt any
filler quantity change before running the plan's SuiteQL probe (0.2) on the
"must be 3" order** — `conversionrate = 1` → filler identity; `rate != 1` →
Phase 2b (unit normalization).

## The variant board (resolution plan 0.3, updated)

| # | Filler entry | Predicted | Actual |
|---|---|---|---|
| 1 | no quantity, empty assignment | `total inventory detail quantity must be N` (unverified) | **field-proven passing** (Transaction A, 2026-10-15); the later "must be 3" failure is now suspected to be a UoM issue (U1) |
| 2 | `quantity: 0`, empty assignment | preferred fix | **REFUTED in field 2026-10-23** — `Value 0.0 outside of valid min/max range for field quantity` |
| 3 | `quantity: 0`, no detail | `INVALID_PARAMETER … InventoryDetail` (per 2026-10-15 replace coverage) | not attempted |
| 4 | filler omitted | unaddressed line ships at pre-filled quantity | not attempted (the original RCA issue 1) |

## What changed

Fillers carry **no `quantity` property** in all three packing IF builders —
`TransferOrderIFPayloadDTO`, `ReturnsIFPayloadDTO`,
`VendorReturnAuthorizationIFPayloadDTO`. `JsonSerializerOptions` uses
`JsonIgnoreCondition.WhenWritingNull` (`NetSuiteApiClientService.cs:216`), so a
null `OrderLineItem.Quantity` is omitted from the JSON entirely and the
serialized filler is byte-identical to the Transaction-A shape:

```json
{"orderLine":1,"itemreceive":false,"inventoryDetail":{"inventoryAssignment":{"items":[]}}}
```

The builders' comments now record the refutation and warn against re-introducing
a filler quantity without field evidence. Phase 1 (reconciliation, paging,
guards) is untouched — the defect was isolated to the Phase 2a filler branch.

## Tests

`dotnet test Integration.NS.Tests` — 575 passing (574 prior + 1 new).

`PackingIFPayloadDTOTests` reworked to pin the wire shape (2c):

- the three `*_filler_serializes_quantity_zero_*` tests became
  `*_filler_omits_quantity_*` — they now assert `quantity` is **absent**;
- `Non_bin_filler_omits_inventory_detail_and_quantity` — the non-bin filler has
  neither property;
- **`UsedBin_filler_serializes_to_the_field_proven_shape` (new)** — asserts the
  exact JSON fragment of a used-bin filler entry, so any wire drift fails the
  build instead of a warehouse operator;
- `Shipping_entries_quantity_matches_assignment_sum_and_fillers_sum_to_zero` —
  the contract test now branches: shipping entries (quantity present) must match
  their assignment sum and name a bin exactly when they ship; fillers (no
  quantity) must sum to 0;
- `Quantity_property_appears_exactly_when_the_entry_ships_something` — replaces
  the old "every entry carries an explicit quantity" assertion.

## Still open

1. **U1 / Phase 2b** — run the SuiteQL probe (`TO-itemFulfillment-resolution-plan.md` 0.2)
   against the order behind `The total inventory detail quantity must be 3`.
   If it is a UoM mismatch, implement 2b (unit normalization) on scanned lines,
   and leave fillers quantity-free permanently.
2. **Sandbox matrix before any future wire-shape change.** The door to variant 3
   and guard-only stays open if variant 1 ever fails; both are localized to the
   filler branch.
3. Verify the refuted order (`STR-THM00100000881/226006`) reloads, re-posts and
   completes on the reverted build — the reconciliation re-reads fresh line
   state, so a partial post by the BAD bucket (which ran before GOOD failed) is
   absorbed on the retry.

## Follow-up the same day — the transform preview is POST-only (405)

The transform-preview verification added above could never read anything:

```
transform=-1 transformError=[NetSuite 405 Method Not Allowed (405):
[INVALID_METHOD] Unsupported HTTP method for the requested re…]
order=ISTR-THM00100000837/223191 open=1584 submitted=28 posted=1584
```

NetSuite's `!transform/itemFulfillment` action is **POST-only**; `GET` answers
405. The guard therefore failed open and the save still posted 1,584 entries
over the REST transform, which rejected them with
`invalid sublist or line item operation` — the same class, one order larger.

Changes made in response:

- `WMS_TO_IF_ENABLE_TRANSFORM_VERIFY` now defaults to **false**. The reader and
  its guards stay in place for the day a supported preview exists (a
  transient-POST preview is *not* proven safe — if `Prefer: transient` were
  ignored it would commit the whole fulfillment), but it no longer costs a
  failing call and a log warning per save.
- `EnsureTOFulfillmentTransformSize` refuses a REST-transform payload above
  `WMS_TO_IF_MAX_TRANSFORM_LINES` (default **1,000**, the REST sublist
  boundary) **before** the POST. The message names both sizes, says nothing was
  saved, and points at the two working paths (NetSuite UI, or the RESTlet).
  Observed failures: 1,022 and 1,584 entries.

## The RESTlet path (the actual fix for large orders)

`TransferOrderIFRestletPayloadDTO` + `SaveTOItemFulfillmentViaRestlet` post to
RESTlet script **2522** with **only the scanned lines** — no fillers, no
`?replace=`, so neither the sublist ceiling nor the empty-assignment removal can
occur. Enabled with `WMS_TO_IF_USE_RESTLET` (default **false**).

Payload contract (confirmed with the NetSuite developer, 2026-10-23):

```json
{
  "transferOrderId": 12345,
  "shipStatus": "B",
  "lines": [
    { "orderLine": 1, "quantity": 10,
      "inventoryAssignments": [
        { "binNumber": 201, "inventoryStatus": 1, "quantity": 6 },
        { "binNumber": 202, "inventoryStatus": 1, "quantity": 4 } ] },
    { "orderLine": 3, "quantity": 5 }
  ]
}
```

Rows are grouped by `orderLine`, so a quality/bin split becomes several
assignments on one line and `quantity` is their sum. Non-bin locations omit
`inventoryAssignments`.

**Gate before enabling:** the contract says nothing about lines absent from
`lines[]`. The path is only safe if the SuiteScript explicitly leaves them
unfulfilled. Pilot on a scratch TO (scan 1 of ~5 lines, confirm only 1 ships),
then enable for the 1,500-line orders.
