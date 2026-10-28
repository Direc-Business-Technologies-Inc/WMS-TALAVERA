# Web PO receiving: `InventoryDetail` missing under `replace=`

Date: 2026-10-28
Status: Code and local regression tests verified; NetSuite sandbox/production validation pending
Reported order: `PO-THM00100000462-TR`

## Symptom and diagnosis

Web receiving reported a NetSuite 400 `[INVALID_PARAMETER]`: resource or
sub-resource `InventoryDetail` named in the `replace` query parameter was not
found in the request body. GOOD failed before any earlier bucket was confirmed;
later buckets were not submitted. A request payload for this particular PO was
not available, so the exact production request cannot be independently confirmed.

The active web path is `ReceivingIntegration.PostItemReceipt` ->
`CreatePOJson(ItemReceiptDTO, int, ...)`, **not** the mobile
`PurchaseOrderIRPayloadDTO` builder. For a selected bin-tracked PO line, web
posting added `?replace=item.inventoryDetail.inventoryAssignment`, but the
builder serialized unreceived open lines (including lines not selected in the
GOOD bucket) as `{ "orderLine": n, "itemreceive": false }` with no
`inventoryDetail`. `WhenWritingNull` removed their null detail property. The
URL and body therefore disagreed. The same error and empty-assignment remedy
were previously observed and field-verified for the mobile PO receipt path
(`.github/specs/2026-10-19-po-item-receipt-filler-inventory-detail-fix.md`).

## Fix and regression boundaries

`Integration.NS/Implementations/Transactions/ReceivingIntegration.cs` now
computes one `replaceInventoryAssignment` flag for both the transform URL and
the active PO payload builder:

| PO entry | Replacement enabled | Serialized inventory assignment |
| --- | --- | --- |
| Received positive quantity | Yes | Existing status, quantity and bin assignments |
| Unreceived/open filler | Yes | `inventoryDetail.inventoryAssignment.items: []`; `itemreceive: false` and no quantity |
| Received positive quantity | No | Existing assignment unchanged |
| Unreceived/open filler | No | No `inventoryDetail`, as before |

The empty list is filtered by `isItemReceived`, so even stale inventory
allocations attached to an unchecked line cannot be forwarded as a receipt.
The existing PO line de-duplication, unchecked fillers, BAD/GOOD/MISSING bucket
order, exhausted-line removal from later transforms, and sequential failure
handling remain in effect. Dropping `replace=` could restore the earlier
"Please configure the inventory detail in line N" error at bin-tracked
locations; omitting fillers could receive lines the operator did not select.
Transfer/Returns RESTlet requests and the mobile PO builder are separate paths.

## Verification and operator recovery

`Integration.NS.Tests/Implementations/Transactions/ReceivingPostingTests.cs`
tests the URL **and** JSON body together for partial bin-tracked PO receipts
with GOOD, BAD and MISSING statuses, including an unchecked line with stale
allocations. It also verifies a plain non-bin partial PO and two sequential
bin-tracked status buckets. Focused run:

```powershell
dotnet test "Integration.NS.Tests/Integration.NS.Tests.csproj" --no-restore --filter "FullyQualifiedName~ReceivingPostingTests"
```

Result on 2026-10-28: 35 passed, 0 failed. These use a capturing fake, not a
live NetSuite transform. Before retrying `PO-THM00100000462-TR`, inspect its
related Item Receipts and reconcile received quantities: the failed request
could have committed if its response was lost. Deploy the web application with
its updated integration assembly, validate a partially received bin-tracked PO
in a sandbox, and confirm quantities, bins and source-order open quantities.
If NetSuite still rejects the request, capture the new error and inspect the
server log's `PostItemReceipt payload` entry before changing the transform URL.
