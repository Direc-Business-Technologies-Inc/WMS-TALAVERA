# Web RTS On-Hand Quantity Display and Guard

> Status: implemented; verify against the NetSuite sandbox before release.
> Scope: web Return to Seller (Supplier Return / Vendor Return Authorization) only.
> Date: 2026-10-28.

## Requirement

Show location On Hand quantity on RTS lines and prevent the allotted quantity from being
negative or greater than On Hand. Mobile RTS behavior is unchanged.

## Implementation

- The RTS line grid is labelled **On Hand Quantity**. Its UoM-normalized value is derived
  from `QuantityOnHand` (`QuantityOnHand / UoM.ConversionRate`). The selected UoM is also
  the UoM used for the allotted quantity field.
- NetSuite RTS and PO line reads carry location `inventoryitemlocations.quantityonhand`
  through `SupplierReturnLineNSDTO` and `SupplierReturnLineDTO` into the web view model.
  The integration query returns the inventory quantity in base/stock units; the web VM
  normalizes it for the selected line UoM once.
- The editable numeric input has a minimum of zero and a maximum of the displayed On Hand
  remaining after allocations on other lines for the same item. A form-level check also
  totals duplicate item lines and reports offending line numbers before submit or approval.
- Barcode scanning invokes the RTS verifier. It compares existing line allocation, already
  staged scans, and the incoming barcode quantity in base units against On Hand. Scans for
  items not yet on the form use the barcode item's On Hand quantity and can still create a
  new line when applied.
- The NetSuite integration re-queries `aggregateitemlocation.quantityonhand` for submitted
  item IDs and the RTS location immediately before create, update, or submit-for-approval.
  It rejects negative quantities, invalid conversion rates, and allocations over current
  On Hand. This avoids relying on the picker grid's short-lived cache for the save guard.

## Verification considerations

Automated build checks should cover `Web.BlazorServer` and `Integration.NS`. Before release,
exercise these cases in the NetSuite sandbox:

1. Create RTS manually at exactly On Hand and one unit over.
2. Create RTS from a PO at exactly On Hand and one unit over.
3. Update an existing RTS after location On Hand has fallen below its allotted amount.
4. Submit an existing RTS for approval after On Hand has fallen below its allotted amount.
5. Scan barcodes up to the On Hand limit, then attempt one more scan; repeat with a non-stock
   barcode UoM and a line UoM with a non-1 conversion rate.
6. Confirm a barcode scan can still add an item that was not previously selected in the grid.

The integration guard is scoped to web Supplier Return create/update/approval operations.
It does not change mobile RTS or other transaction types.
