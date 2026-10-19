# Possible NetSuite 4xx Errors — Mobile-Reachable Paths

> Status: documentation only. No code changed.
> Branch: `fix/mob-backlogs` @ `5a76cb58` (includes `85c94246` IF shape fix).
> Scope: NetSuite-side 400-class (validation/business) errors reachable from
> Mobile.MAUI save paths. 401/429/5xx are already classified as transient/auth
> in `Integration.NS/Exceptions/NetSuiteExceptionFactory.cs` and
> `Mobile.MAUI/Services/ApiClientService.cs` (`NotifyUnauthorizedAsync`).

All 11 mobile save paths follow the same chain:

```text
Mobile.MAUI --POST--> Api.CoreWebAPI --MediatR Cmd--> NetSuiteApiClientService --REST/Restlet--> NetSuite
```

None of the paths below has a pre-NetSuite guard for the listed cases.
NetSuite returns **400** and the command handler wraps it as
`ApiResult.ServerError("Error while posting ...")`.

---

## 1. Packing — Item Fulfillment (highest likelihood; same family as reported error)

Reported error for reference:

```text
Action failed: Error while posting GOOD TO Item fulfillment: NetSuite 400 Bad Request:
Bad Request(400): [USER_ERROR]: Error while accessing a resource.
Please configure the inventory details in line 5 of the item list.
```

(Note: current code at `NetSuiteApiClientService.cs:1108` reads
`GOOD TO Item Fulfillment` capital-F; the log's lowercase-f suggests an older
build or transcription. Same throw site: `SaveTOItemFulfillment()` GOOD branch.)

| Mobile entry | API route | Handler and NetSuite call |
|---|---|---|
| `Packing/TransferOrder/TransferOrderItemView` then `POST /Packing/TransferOrder/SaveScan` | `Api.CoreWebAPI/Controllers/Packing/TransferOrderController.cs` | `PostTransferOrderIFCmd` then `SaveTOItemFulfillment` then `POST .../transferOrder/{id}/!transform/itemFulfillment?replace=item.inventoryDetail.inventoryAssignment` |
| `Packing/Returns/ReturnsItemView` then `POST /Packing/Returns/SaveScan` | `Controllers/Packing/ReturnsController.cs` | `PostReturnsIFCmd` then `SaveReturnsItemFulfillment` then same transform on `transferOrder/{id}` |
| `Packing/VRA/VendorReturnAuthorizationItemView` then `POST /Packing/VendorReturnAuthorization/SaveScan` | `Controllers/Packing/VendorReturnAuthorizationController.cs` | `PostVendorReturnAuthorizationIFCmd` then `SaveVRAItemFulfillment` then `POST .../vendorReturnAuthorization/{id}/!transform/itemFulfillment?replace=...` |

Payload builders (`Application.DataTransferObjects/.../Packing/NS/Payload/`):
`TransferOrderIFPayloadDTO.cs`, `ReturnsIFPayloadDTO.cs`,
`VendorReturnAuthorizationIFPayloadDTO.cs`. Shared shape in
`Transactions/Commons/NS/Payload/PayloadsDTO.cs`
(`ItemContainer` / `OrderLineItem` / `InventoryDetail` / `InventoryAssignment`).

### 1a. USER_ERROR: Please configure the inventory details in line N (residual)

Trigger: a scanned line with `IsLocationUsedBin == true` but both
`NetsuiteMaterialVendorAssignedBin == 0` and
`NetsuiteMaterialPrefferedBinId == 0` (`TransferOrderIFPayloadDTO.cs:51-62`,
same pattern in Returns/VRA builders). `BinNumber` serializes as null and is
omitted (`JsonSerializerOption` is `WhenWritingNull`,
`NetSuiteApiClientService.cs:191-196`), so NetSuite receives
`{inventoryStatus, quantity}` with no `binNumber` for a bin-tracked location
and rejects the line. Same outcome for any lot/serial-tracked item:
`InventoryAssignmentItem` (`PayloadsDTO.cs:67-77`) has no lot/serial fields at
all, so there is no way to satisfy NetSuite's inventory-detail requirement
for those items today.

Why it survives `85c94246`: that commit fixed the systematic shape bug
(`inventoryAssignmentList` wrapper then flat `inventoryAssignment`, plus
re-adding `?replace=item.inventoryDetail.inventoryAssignment` to
`ItemFulfillmentUrl` / `ItemFulfillmentUrlNotUsedBin`). The missing-bin-data
case is orthogonal and still posts a well-shaped but incomplete line.

`line N` = Nth entry in the posted `item.items[]` array (includes
`ScannedQuantity == 0` lines sent as `{orderLine, itemreceive:false}`), not
necessarily `OrderLine == N`.

### 1b. Duplicate orderLine

Trigger: one `LineSequenceNumber` scanned as both good and bad arrives as two
rows. The IF builders use bare `.Select(...)` with no `GroupBy`, so two
`item.items[]` entries share one `orderLine` and NetSuite rejects with 400
duplicate-line. Contrast `PurchaseOrderIRPayloadDTO.cs:42`, which groups by
`LineSequenceNumber` and picks the per-post row; the IF path never got the
equivalent treatment.

### 1c. Zero-quantity filler lines shift the reported index

`ScannedQuantity == 0` lines are posted as `{orderLine, itemreceive:false}`
with no quantity. Benign in isolation, but they occupy positions in
`item.items[]`, so the `line N` in a NetSuite error does not map 1:1 to a
scanned line and misleads triage.

### 1d. GOOD-branch posts even when everything is zero

`SaveTOItemFulfillment` (`NetSuiteApiClientService.cs:1094-1110`): the BAD
branch requires `badTO.Any(x => x.ScannedQuantity > 0)`, but the GOOD branch
only checks `goodTO.Any()`. A GOOD list whose every line scanned zero still
POSTs a transform body NetSuite rejects with 400.

### 1e. Wrong shipStatus from a stale exemption flag

`PostTransferOrderIFCmd` / `PostReturnsIFCmd` /
`PostVendorReturnAuthorizationIFCmd` fail closed when the trip-ticket
exemption lookup returns null (refuse save), but when it resolves they stamp
`IsTripTicketExempt` onto every line unchecked. A stale flag posts
`B` (Packed) vs `C` (Shipped) against NetSuite's actual order state and the
transform fails a business-rule 400.

Guard status: shape fixed (`85c94246`); **no bin-presence check, no dedupe, no
all-zero guard** in the IF path.

---

| Mobile entry | API route | Handler and NetSuite call |
|---|---|---|
| `Receiving/PurchaseOrder/PurchaseOrderItemView` then `POST /Receiving/PurchaseOrder/SaveScan` | `Controllers/Receiving/PurchaseOrderController.cs` | `PostPurchaseOrderIRCmd` then `SavePOItemReceipt` then `POST .../purchaseOrder/{id}/!transform/itemReceipt` (+`?replace=item.inventoryDetail.inventoryAssignment` at bin-tracked locations) |
| `Receiving/TransferOrder` views then `POST /Receiving/TransferOrder/SaveScan` | `Controllers/Receiving/TransferOrderController.cs` | `PostTransferOrderIRCmd` then `SaveTOItemReceipt` then Restlet `script=1853` |
| `Receiving/Returns` views then `POST /Receiving/Returns/SaveScan` | `Controllers/Receiving/ReturnsController.cs` | `PostReturnsIRCmd` then `SaveReturnsItemReceipt` then `POST .../transferOrder/{id}/!transform/itemReceipt?replace=...` |

Builders: `Receiving/NS/Payload/PurchaseOrderIRPayloadDTO.cs`,
`TransferOrderIRRestletPayloadDTO.cs`, `ReturnsIRPayloadDTO.cs`.

### 2a. Missing-bin 400 (same root as 1a)

`PurchaseOrderIRPayloadDTO.cs:81-92` and `ReturnsIRPayloadDTO.cs:66-77` use
the identical `IsLocationUsedBin ? vendorBin : preferredBin : null` chain.
Binned location + both bin IDs `0` then `inventoryDetail` without `binNumber`
then `Please configure the inventory details in line N` on the receipt
transform.

### 2b. Returns receipt does not dedupe; PO does

PO groups by `LineSequenceNumber` per post
(`PurchaseOrderIRPayloadDTO.cs:42`) and is safe from 1b-style duplicates.
`ReturnsIRPayloadDTO.cs:43` is a bare select then duplicate `orderLine` 400
on good+bad split scans, same as 1b.

### 2c. TO receipt Restlet posts lines against one fulfillment ID

`SaveTOItemReceipt` (`NetSuiteApiClientService.cs:957`) takes `IFOrderId`
from the first line's `NetsuiteOrderInternalId`. A mixed-IF scan posts lines
under the wrong fulfillment then Restlet-side 400. `TOxItemView:200-205`
reads `UserId` from SecureStorage in `OnAfterRender`, after the save action
is built, so a fast save can post `receiverEmployeeId: 0`.

### 2d. UserId / EmployeeId == 0

PO, TO-Restlet, and Returns forward the mobile-supplied `UserId` unchecked.
`0` / missing then NetSuite received-by reference-validation 400.

Guard status: PO dedupes, Returns does not; **no bin-presence, no `UserId > 0`,
no IF-homogeneity check** anywhere in this path.

### 2e. `replace=` resource missing from the body (fillers)

`[INVALID_PARAMETER] Resource or sub-resource 'InventoryDetail' from the query
parameter replace was not found in the request body`. Whenever the transform URL
carries `?replace=item.inventoryDetail.inventoryAssignment`, NetSuite requires
that sub-resource on **every** `item.items[]` entry. Zero-qty filler lines
(`{orderLine, itemreceive:false}`) emitted without `inventoryDetail` therefore
reject the whole save — the same failure class as packing case 1c/1a. Hit a
**partially scanned** bin-tracked PO (diag `scannedLines=31 totalLines=52`); a
fully scanned order (no fillers) passed, so it looked intermittent.

**Fixed and field-verified 2026-10-19:** PO fillers at bin-tracked locations now
carry an empty assignment (`inventoryAssignment.items: []`), gated on the same
`isUsedBin` that builds the `?replace=` URL; Returns fillers carry it
unconditionally (its `?replace=` is unconditional). Spec:
`.github/specs/2026-10-19-po-item-receipt-filler-inventory-detail-fix.md`.
**Not yet exercised:** a partial Returns scan.

---

## 3. Trip Ticket — CreateTripTicketView then POST /TripTicket/SaveScan

Chain: `TripTicketController.TripTicketSaveScan` then `PostTripTicketCmd`
then `SaveTripTicket` = `ChangeShipStatus()` (PATCH each IF to shipped) then
Restlet `script=1862` POST (`TripTicketPayloadDTO.CreateTripTicket`).

### 3a. Empty ItemFulfillments

Mobile guards in the dialog-confirm flow (`CreateTripTicketView.cs:172-176`),
but a direct/double submit with zero scanned IFs builds `lines: []` then
Restlet 400.

### 3b. Unpicked required references

`TripTicketPayloadDTO.cs:56-98` dereferences with `!` (`FromSubsidiary!`,
`Driver!`, `Helper!`, `TruckPlateNumber!`, `OriginLocation!`);
`TripTicketDetailsView.cs:219-235` (`OnConfirm`) copies `Model.*` verbatim
with no required-field validation. A missing pick then client NullReference
(500) or an `"id": "0"` reference then NetSuite 400 invalid reference.

### 3c. Non-atomic ship-status flip

`ChangeShipStatus` (`NetSuiteApiClientService.cs:1276-1297`) PATCHes each IF
in a loop with no transaction. If IF #2 fails, IF #1 is already Shipped; a
retry re-PATCHes an already-shipped IF then NetSuite 400.

### 3d. Stale packed list

`GET /TripTicket/ItemFulfillment/Packed` loads once at view init; an IF that
moves packed to shipped elsewhere between scan and save then 400 on PATCH.

Guard status: **no required-field validation on the details dialog, no
atomicity** on the ship-status flip.

---

## 4. Inventory Counting — InventoryCountingItemView then POST /InventoryCounting/SaveScan

Chain: `InventoryCountingController.InventoryCountSaveScan` then
`PatchInventoryCountingCmd` then `PatchInventoryCounting` then
`PATCH .../inventorycount/{orderId}` with
`InventoryCounting/NS/Payload/PatchInventoryCountingPayloadDTO.cs`.

### 4a. orderId == 0

`PatchInventoryCounting` (`NetSuiteApiClientService.cs:1306`) takes the order
id from the first line. An empty/null list then `PATCH
.../inventorycount/0` then 404/400. Mobile `ActionSaveScan`
(`InventoryCountingItemView.cs:142-157`) posts `ICItems` unconditionally and
`SaveScan()` (`:340-376`) rebuilds the list with no empty check.

### 4b. Wrong countLine

`CountLine = LineSequenceNumber` assumes the mobile line number is NetSuite's
count-line number; any renumbering after a recount then `INVALID_LINE` 400.

### 4c. Bad-quantity silently dropped

The service-side filter is named `goodIC = !IsBad` and only non-bad lines are
patched. The operator sees bad scans accepted on-device while NetSuite never
receives them; surfaces later as recount-mismatch 400s.

### 4d. All-zero patch

No `ScannedQuantity > 0` filter; an all-zero list still PATCHes.

---

## 5. Inventory Worksheet — CreateInventoryWorksheetView then POST /InventoryCounting/Worksheet/SaveScan

Chain: `InventoryCountingController.InventoryWorksheetSaveScan` then
`PostInventoryWorksheetCmd` then `PostInventoryWorksheet` then worksheet
Restlet with
`InventoryCounting/NS/Payload/PostInventoryWorksheetPayloadDTO.cs`
(hardcoded `account = 1`, `department = 15`, `class = 2`,
`lastinday = "T"`).

### 5a. Bin = null on a bin-tracked location

Builder sends `Bin = null` when `NetsuiteBinInternalId == 0`. Mobile
`SaveScan()` (`:546-581`) does guard (location required, bin required when
`RequiresBinSelection`, no empty list, no negatives), the best-guarded path
in this catalog, but a worksheet saved without bin scope against a
bin-tracked location still 400s server-side.

### 5b. Zero-line payload

The builder filters to positive scans only, so an all-zero scan posts a
valid header with `lines: []` then Restlet 400.

### 5c. Hardcoded header IDs

`account / department / class` are constants; a subsidiary whose chart
differs then 400 invalid reference. No per-subsidiary lookup exists.

### 5d. Duplicate item+bin rows merged

Grouped by item only, not item+bin. Two bins for one item collapse into one
`inventoryDetail` per status, losing the bin split, then NetSuite
bin-balance 400.

---

## 6. Cross-cutting (all mobile writes)

### 6a. Zero/blank order identity

No mobile or API check that `NetsuiteOrderInternalId != 0` /
`OrderNumber != ""` before building transform URLs.
`.../purchaseOrder/0/...`, `.../transferOrder/0/...` then 404/400 surfaced as
`ApiResult.ServerError`.

### 6b. Stale lines on double-submit / refresh-after-scan

Mobile `Refresh()` snapshots scanned state and re-posts full lists; NetSuite
rejects already-fulfilled/received lines then 400 on retry. Only packing has
partial over-scan toasts; nothing revalidates `NSLineQuantity*` at save time.

### 6c. ApiClientService hides raw 4xx detail

`Mobile.MAUI/Services/ApiClientService.cs` special-cases only 404 and 401.
Any other non-success (400/403/409/422) is deserialized straight into
`ApiResult<T>`; when NetSuite's raw 400 body is not `ApiResult`-shaped, the
operator sees a generic/deserialization error instead of the message.

### 6d. Reads-by-POST with unready parameters

Lookups (`PendingFulfillment`, `Items`, `Barcodes`) POST anonymous bodies.
List views build the query action in `OnInitialized` but only load
`UserSubsidiaryId`/`UserId` from SecureStorage in `OnAfterRender` (e.g.
`Packing/TransferOrder/TransferOrderView.cs:26-63`), so the first attempt can
post zeros then SuiteQL 400 (`UnsupportedSearch` / `OffsetOutOfBounds`, as
classified in `NetSuiteErrorCodes.cs`), user-visible even though categorized.

---

## Suggested verification order (no changes made)

1. Repro 1a/2a with a bin-tracked location and zeroed bin IDs; expect the
   exact `inventory details in line N` 400.
2. Repro 1b/2b (Returns) with a good+bad split scan on one line.
3. Repro 3b with an unpicked Driver/Origin on a Trip Ticket.
4. Repro 4a with an empty count post; 5b with an all-zero worksheet.


