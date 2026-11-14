# Return to Supplier: restrict items to the selected preferred vendor

## Behavior

In WMS Web Return to Supplier, the item picker only returns items whose item-master preferred vendor for the selected warehouse's subsidiary matches the selected header vendor. A scanned barcode is rejected when its subsidiary-scoped preferred vendor does not match. The API repeats the check before create, update, and approval so stale UI state or a direct caller cannot submit a mismatched item. For standard create/update requests, each line vendor must also match the selected header vendor because that is the vendor actually written in the line payload.

Items without a preferred vendor are ineligible. When the selected vendor, subsidiary, or warehouse changes, staged scans and row selection are cleared; the existing confirmation and line-clearing behavior for vendor/subsidiary changes is preserved.

## Implementation notes

- The picker reuses the existing subsidiary-scoped `PreferredVendorId` projection and adds the preferred-vendor filter to its `DataGridIntent`. It does not alter the shared item query or introduce an unscoped `itemvendor` join, preserving duplicate-free behavior for other item-picker consumers.
- Barcode lookup uses the existing `PreferredVendorForLocation` query shape; subsidiary scoping remains inside the derived table because moving that predicate to the outer join caused NetSuite `INVALID_PARAMETER` errors.
- `SupplierReturnIntegration` validates each line item against the selected vendor using the actual subsidiary of that line's location, and rejects a selected subsidiary/location mismatch. Multiple preferred-vendor rows for one item in that subsidiary fail closed rather than choosing an arbitrary vendor.
- Direct standard-create, update, approval, and PO-transform integration writes all invoke validation at their own write boundary. For PO transforms, the validator fetches the source PO's header and item lines by internal ID, confirms its actual vendor and subsidiary, and validates those actual source lines before posting the transform, rather than trusting the browser's line list. Existing source-line vendor values, when present, must match the selected header vendor.

## Verification

`SupplierReturnPreferredVendorValidationTests` covers accepted and rejected standard writes, line/header vendor mismatch, location/subsidiary mismatch, source-PO header and item validation, approval rejection, and that the picker filter resolves to the preferred-vendor SQL column. Manual UI verification should check matching/mismatching items in the picker, scan rejection, and clearing of staged scans after changing vendor/location/subsidiary.
