# Fix: PO Item Receipt fillers omit `inventoryDetail` under `?replace=` (INVALID_PARAMETER on partial scans)

Date: 2026-10-19
Status: Fix in source, builds clean (`Integration.NS.Tests`), payload + full suite green — **awaiting client verification**
Mode: Bugfix / RCA / Follow-up to [2026-10-17 PO `?replace=` fix](2026-10-17-po-item-receipt-missing-inventory-assignment-replace.md) and [2026-10-19 PO line fan-out & fillers fix](2026-10-19-po-item-receipt-line-fanout-and-fillers-fix.md)
Scope: Receiving module — GOOD/BAD PO Item Receipt (`SavePOItemReceipt` → `purchaseOrder/{id}/!transform/itemReceipt`) payload builder, and the latent identical defect in the Returns Item Receipt builder
Related:
- [2026-10-15 IF `replace=` / INVALID_PARAMETER fix](2026-10-15-if-replace-param-invalid-parameter-fix.md) — **the same error on the fulfillment transform and the same remedy (empty assignment on fillers), field-verified**
- [2026-10-19 PO line fan-out & fillers fix](2026-10-19-po-item-receipt-line-fanout-and-fillers-fix.md) — introduced the "post every open line" mobile payload; its assumption that the `itemReceipt` transform tolerates a detail-free filler is corrected here
Changed files:
- `Application.DataTransferObjects/Transactions/Receiving/NS/Payload/PurchaseOrderIRPayloadDTO.cs`
- `Application.DataTransferObjects/Transactions/Receiving/NS/Payload/ReturnsIRPayloadDTO.cs`
- `Integration.NS/Services/NetSuiteApiClientService.cs` (`SavePOItemReceipt` call sites)
- `Integration.NS.Tests/Payloads/PurchaseOrderIRPayloadDTOTests.cs`

## Summary

Receiving a **partially scanned** purchase order at a bin-tracked location fails the last step:

```
Action failed: Error while posting GOOD PO Item Receipt. NetSuite 400 Bad Request:
Bad Request(400): [INVALID_PARAMETER] Resource or sub-resource 'InventoryDetail' from the
query parameter replace was not found in the request body. Provide a valid resource name
in the query parameter or include the resource in the request body.
[diag replace=True usedBin=True scannedLines=31 totalLines=52 build=1.0.0+5598a5da…]
```

The `?replace=` parameter is present (`replace=True`), so this is **not** the sibling defect the
2026-10-17 spec fixed. It is the same failure the 2026-10-15 spec traced on the
`itemFulfillment` transform, now reproduced on `itemReceipt`:

**On a bin-tracked location the transform URL declares
`replace=item.inventoryDetail.inventoryAssignment`, so every entry in the body's `item.items[]`
must carry that sub-resource. The body carried it on only the 31 scanned lines; the 21 unscanned
zero-qty fillers were emitted as `{orderLine, itemreceive:false}` with no `inventoryDetail`, and
NetSuite rejected the whole transform.**

## Root Cause

Two individually-correct fixes were incoherent together:

| Change | Effect |
|---|---|
| 2026-10-17: `BuildPOItemReceiptUrl` appends `?replace=item.inventoryDetail.inventoryAssignment` for bin-tracked locations | NetSuite now replaces the line's assignment with whatever the body says — and validates that the named sub-resource exists on the body's item entries |
| 2026-10-19: mobile `SaveScan` posts **every open line** (Req 2.1), unscanned lines as detail-free fillers | `item.items[]` now mixes entries **with** `inventoryDetail` (scanned) and **without** (fillers) |

`PurchaseOrderIRPayloadDTO` returned fillers with no `inventoryDetail`:

```csharp
if (!isReceived)
{
    return new OrderLineItem { OrderLine = line.LineSequenceNumber, isReceived = false };
}
```

The two changes never met until a PO was scanned in part, which is why the flow "worked at first":

- **Old APK** posted only the scanned lines → `totalLines == scannedLines` → every entry carried `inventoryDetail` → the replace validation passed.
- **New APK** (Req 2.1) posts the open set → `totalLines > scannedLines` → the detail-free fillers trip the validation.

A **fully scanned** order still passes (no fillers) — which is why the failure looked intermittent.

### The 2026-10-19 "tolerates a detail-free filler" assumption is refuted

The 2026-10-19 spec chose to *leave PO fillers detail-free*, resting on *"the `itemReceipt`
transform tolerates a detail-free filler — proven by the production Returns receipt."* That
inference was never observed: the Returns receipt is evidently only ever exercised with **every**
line scanned, where no filler exists. This screenshot is the first field evidence of the
`itemReceipt` transform enforcing the same strict replace validation as `itemFulfillment`. The
Returns builder (`ReturnsIRPayloadDTO.cs:45`) carries the **identical latent defect** —
`SaveReturnsItemReceipt` always posts with `?replace=`, so a partially scanned returns receipt
would fail the same way. Both are fixed here.

## The Fix

Mirror the fulfillment builders' field-proven remedy (empty assignment on fillers), gated on the
same flag that builds the URL.

### PO receipt

`PurchaseOrderIRPayloadDTO.CreateForItemReceipt` gains an `isUsedBin` parameter; fillers carry an
empty assignment when the location is bin-tracked, exactly like `TransferOrderIFPayloadDTO.cs:41-50`:

```csharp
var filler = new OrderLineItem
{
    OrderLine = line.LineSequenceNumber,
    isReceived = false
};

if (isUsedBin)
{
    filler.InventoryDetail = new InventoryDetail
    {
        InventoryAssignment = new InventoryAssignment { Items = [] }
    };
}

return filler;
```

`SavePOItemReceipt` passes the **already-computed** `isUsedBin` (`Data.Any(x => x.IsLocationUsedBin)`)
to both the GOOD and BAD builders, so the URL's `?replace=` and the body's coverage can never drift.
Non-bin posts are byte-identical to today (no `replace`, no filler detail).

### Returns receipt (latent, same defect)

`SaveReturnsItemReceipt` posts with an unconditional `?replace=`, so `ReturnsIRPayloadDTO` fillers
now carry the empty assignment **unconditionally** — the sub-resource must exist on every entry
regardless of bin tracking. No signature change.

## Behaviour Matrix (after fix)

| Location | Scanned line | Filler line (qty 0) | URL | Status |
|---|---|---|---|---|
| Bin-tracked | `+ inventoryDetail.inventoryAssignment[status, qty, binNumber]` | `+ inventoryDetail.inventoryAssignment.items: []` | `?replace=…inventoryAssignment` | **Awaiting client repro** |
| Not bin-tracked | `+ inventoryDetail.inventoryAssignment[status, qty, binNumber:null]` | `{orderLine, itemreceive:false}` (no detail) | plain transform | unchanged from today |
| Returns (any) | `+ inventoryDetail.inventoryAssignment[...]` | `+ inventoryDetail.inventoryAssignment.items: []` | `?replace=…inventoryAssignment` | **Awaiting client repro** |

## Tests

- `PurchaseOrderIRPayloadDTOTests.Bin_tracked_orders_cover_every_entry_with_inventory_detail`
  (theory, GOOD + BAD): a partially scanned bin-tracked order emits `inventoryDetail` on **every**
  entry — the scanned line and the empty-assignment filler.
- `PurchaseOrderIRPayloadDTOTests.Non_bin_orders_leave_filler_lines_without_inventory_detail`:
  the non-bin filler shape is unchanged.
- `PackingIFPayloadDTOTests` (Returns IR agree-test) and the existing PO payload suite stay green.
- Full `Integration.NS.Tests`: **425 passed / 1 failed** — the single failure is
  `NetSuiteApiClientServiceTokenTests.HttpClient_has_explicit_timeout_of_100_seconds_by_default`, a
  known timing-sensitive test that passes in isolation (same behaviour on the pristine tree).

## Verification Plan

Must be confirmed on the client; no NetSuite sandbox was available in this session.

1. **A partially scanned bin-tracked PO** (the field case: `totalLines > scannedLines`) — must post,
   and the receipt must land in the resolved bin.
2. **A fully scanned bin-tracked PO** — must still post (no regression).
3. **A non-bin PO** — must still post with the plain transform (unchanged shape).
4. **A partially scanned returns receipt** — must post (latent defect closed).
5. **Any remaining failure** — the diag must carry `build=…`; a body-level
   `INVALID_PARAMETER … not found in the request body` with `replace=True` means a filler still
   escaped coverage.

## Decisions and Alternatives Rejected

| Decision | Alternative rejected and why |
|---|---|
| **Empty assignment on fillers, gated on the URL's `isUsedBin`** (chosen) | *Make it unconditional for PO too*: adds `inventoryDetail` to non-bin posts, contradicting the 2026-10-15 "legacy shape, no detail" decision that is proven for non-bin. |
| **Pass `isUsedBin` into the builder** (chosen) | *Recompute it inside the builder*: works, but two independent computations of a URL-driving flag is exactly how body and URL drifted here. |
| **Fix Returns IR now** (chosen) | *PO only, note Returns as latent*: `SaveReturnsItemReceipt` already ships an unconditional `?replace=`, so the same field failure is one partial returns scan away. |
| **Keep `?replace=`** (chosen) | *Drop it*: reinstates the 2026-10-17 defect ("Please configure the inventory detail in line N"). |

## Residual Risk

1. **Medium — not verified live.** The remedy is field-proven on `itemFulfillment` (2026-10-15
   Transaction A passed with `items: []`), but `itemReceipt` acceptance of the empty filler
   assignment is inferred from the same transform family, not observed.
2. **Low — BAD-before-GOOD with no compensation** (pre-existing, unchanged). A failed GOOD post can
   leave the PO half-received; reconcile in NetSuite before re-testing.
3. **Low — the `[diag …]` markers stay** until the receiving flows sign off.

## Follow-up Actions

| # | Action | Status |
|---|---|---|
| 1 | Empty-assignment filler coverage in `PurchaseOrderIRPayloadDTO` | **Done** |
| 2 | Pass the URL's `isUsedBin` into the PO builder | **Done** |
| 3 | Empty-assignment filler coverage in `ReturnsIRPayloadDTO` | **Done** |
| 4 | Regression tests (bin-tracked full coverage, non-bin unchanged) | **Done** |
| 5 | Verify a partially scanned bin-tracked PO posts, in the correct bin | Open — client |
| 6 | Verify a partially scanned returns receipt posts | Open — client |
| 7 | Correct the 2026-10-19 spec's "itemReceipt tolerates detail-free filler" assumption | **Done** — correction note added |
