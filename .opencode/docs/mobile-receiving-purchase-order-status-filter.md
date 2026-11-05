# Mobile Receiving: hide closed purchase orders

## Behavior

The mobile Receiving purchase-order list uses
`Integration.NS/NSScripts/NS_PurchaseOrder_Get_PendingReceipt.sql`. It now excludes
orders whose native NetSuite status is `G` (Closed) and whose admin-managed
`custbody_dbti_custom_approval_status` is `20` (Closed). The NetSuite admin
confirmed the custom status meaning.

The list's existing native status allowlist (`B`, `E`) already excludes `G`; the
explicit `status != 'G'` predicate documents and preserves the closed-state rule.
Custom approval status is excluded with a null-preserving condition, so unset
approval statuses remain eligible under the other existing filters.

The mobile purchase-order number lookup uses
`NS_PurchaseOrder_Get_ByOrderNumber.sql` and applies the same eligibility rules,
so a closed PO cannot be opened through direct lookup even if it is not in the list.
The web Receiving PO grid applies the equivalent custom-status exclusion in
`ReceivingIntegration.GetPurchaseOrdersListAsync`.

## Verification

The mobile SQL scripts are exercised through the application query names
`NS_PurchaseOrder_Get_PendingReceipt` and `NS_PurchaseOrder_Get_ByOrderNumber`.
Validate in the configured NetSuite sandbox with an open PO, a PO with custom
approval status `20`, and a PO with native status `G`; only eligible open orders
should appear or resolve by number.
