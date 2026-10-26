# WMS Business Rules Catalogue

This document consolidates the business rules and operational logic currently visible in the repository.

It is intended to be easy to read for product owners, warehouse users, developers, testers, and support teams. It describes what the system currently appears to do, where the rule is enforced, and where the repository contains uncertainty or conflicting descriptions.

## How to Read This Document

Each rule is labelled using the following terms:

- **Enforced**: executable code or a database query applies the rule.
- **Tested**: a checked-in automated test asserts the rule. Tests were not necessarily executed during this review.
- **UI rule**: a screen prevents or allows the action, but the server or integration may not independently enforce it.
- **Documented**: described in a process document or comment, without sufficient evidence of runtime enforcement.
- **Legacy**: associated with an older SAP path or superseded documentation.
- **Unresolved**: the repository identifies a gap, contradiction, or behavior that needs confirmation.

File references point to the source of the rule. Line numbers may change as the code evolves.

## Core Concepts

### Systems of record

- MSSQL owns administration, settings, document numbering, and local inventory-counting documents.
- NetSuite owns the active transfer, fulfillment, trip-ticket, receiving, return, and approval workflows.
- SAP integrations remain in the repository but are documented as legacy or not registered in the active process flow.
- A local application transaction does not create a distributed transaction with NetSuite or SAP. A database rollback cannot undo an ERP write that already succeeded.

Source: `.opencode/docs/WMS_PROCESS_FLOW.md:37-62,675-695`, `Application.UseCases/Behaviors/TransactionalDocumentBehavior.cs:6-32`.

### Status vocabulary

The same letter or number must not be assumed to have the same meaning in every workflow.

| Context | Value | Meaning |
| --- | --- | --- |
| NetSuite approval | `1` | None / initial |
| NetSuite approval | `2` | Draft |
| NetSuite approval | `3` | Rejected |
| NetSuite transaction | `B` | Pending receipt |
| NetSuite transaction | `D` | Partially received |
| NetSuite transaction | `E` | Pending billing |
| NetSuite transaction | `F` | Fully billed |
| Item Fulfillment | `B` | Packed, awaiting trip ticket |
| Item Fulfillment | `C` | Shipped |
| Inventory allocation | `1` | Good |
| Inventory allocation | `3` | Bad / confiscated |
| Inventory allocation | `6` | Missing |
| Local count document | `Open` | Can be saved |
| Local count document | `Saved` | Can be recounted or posted |
| Local count document | `Posted` | ERP posting completed |
| Local count document | `Recount` | Recount state |

Source: `.opencode/docs/WMS_PROCESS_FLOW.md:128-197,547-567`.

## Access and Permissions

### Login and account state

- Unknown users cannot log in.
- Inactive users cannot log in.
- Locked users cannot log in when lockout is enabled.
- Failed passwords increment the current day's attempt count.
- The account is locked when the attempt count becomes greater than the configured maximum, not when it merely reaches the maximum.
- The default maximum is five attempts when the setting is absent or invalid.
- A successful login resets the attempt count and unlocks the account.
- The configured setting code is `wms.max_login_attemts`.

Status: **Enforced**.

Source: `Application.UseCases/Commands/System/Authentication/LoginCmd.cs:25-62`.

### Password and permission caveats

- The property named `HashedPassword` is decrypted and compared with the supplied password. The property name does not prove that the value is a one-way hash.
- Seed data contains accounts with a shared literal password. This conflicts with the security guidance that credentials should not be hardcoded.
- Web permission claims are loaded during login. Permission changes generally take effect on the next login rather than immediately in an existing session.
- Web authentication uses a claims cookie. The mobile API is documented separately as using JWT authentication.
- A permission check on a page or menu is not automatically proof that the corresponding integration write is protected.

Status: **Enforced/documented with security concerns**.

Source: `Application.UseCases/Commands/System/Authentication/LoginCmd.cs:48-56`, `Database.MsSql/Configurations/AppDefaults.cs:126-172`, `.claude/auth.md:87-109`, `Web.BlazorServer/Components/Security/AuthenticationController.cs:44-93`.

### Warehouse, subsidiary, and location scope

- Login may enrich claims with allowed locations and a subsidiary based on the linked NetSuite employee.
- Location and subsidiary filters are applied only by methods that explicitly use the filtering helpers.
- Missing or empty allowed-subsidiary values can leave a query unchanged rather than denying it.
- The STR header-by-reference query has a subsidiary filter commented out.
- Therefore, location and subsidiary scoping is not a single repository-wide invariant.

Status: **Partially enforced; unresolved scope gap**.

Source: `Web.BlazorServer/Components/Security/AuthenticationController.cs:76-91`, `Integration.NS/Helpers/SuiteQLQueryExtensions.cs:72-110`, `Integration.NS/Implementations/Transactions/StockTransferRequestIntegration.cs:263-276`.

## Stock Transfer Requests and Inventory Transfer Requests

### Stock Transfer Request creation

An STR requires:

- At least one line.
- Allotted quantity no greater than available or on-hand quantity.
- A Preferred Vendor on every line.
- Requested By.

The STR is created in NetSuite with WMS-origin metadata and starts with order status `A`.

Status: **UI-enforced/documented**.

Source: `Web.BlazorServer/Components/Pages/Transaction/StockTransferRequest/Components/STRForm.razor.cs:98-140`, `.opencode/docs/WMS_PROCESS_FLOW.md:248-257`.

### STR edit and approval rules

- An STR is considered editable when it has not been submitted for approval and its NetSuite approval status is Draft (`2`) or Rejected (`3`).
- Submission sets `custbody_dbti_submitted_for_approval` to true.
- Approval and rejection are performed by a NetSuite workflow, not by a WMS approval command.
- Rejected requests become editable again.
- The screen checks editability, but the inspected PATCH methods do not independently revalidate it before writing.

Status: **Read/UI rule; write-side enforcement unresolved**.

Source: `.opencode/docs/WMS_PROCESS_FLOW.md:128-145,263-273`, `Integration.NS/Implementations/Transactions/StockTransferRequestIntegration.cs:393-426`.

### Inventory Transfer Request creation

An ITR requires:

- At least one line.
- A bin assignment for every line.
- Allotted quantity no greater than available quantity.

An ITR is itself the NetSuite inventory-transfer record. There is no separate WMS posting step.

Status: **Documented/UI validation**.

Source: `.opencode/docs/WMS_PROCESS_FLOW.md:438-455`.

### Purchase-category visibility

The shared purchase-category rule is:

- Trade transactions pass.
- Categories other than Non-Trade category `3` pass without subcategory filtering.
- Non-Trade category `3` passes only when the subcategory is one of:
  - Office Supplies: `4`
  - Store Supplies: `5`
  - Uniforms: `9`
- A null category or null subcategory does not pass merely because it is not equal to `3`; the in-memory implementation intentionally mirrors SQL three-valued logic.
- Receiving pushes the predicate into SuiteQL.
- STR grids evaluate the rule in memory because applying the predicate in NetSuite caused a severe performance problem.

Status: **Enforced/tested**.

Source: `Integration.NS/Helpers/PurchaseCategoryRules.cs:12-29,54-72`, `Integration.NS.Tests/Implementations/Transactions/PurchaseOrderListingCategoryFilterTests.cs:39-96`, `STR-purchase-category-filter-summary.md:13-19`.

Known limitation: STR Returns are outside the shared purchase-category filter.

Source: `STR-purchase-category-filter-summary.md:175-189`.

## Packing and Item Fulfillment

### Packing candidate selection

An ordinary transfer or intercompany transfer can appear in the packing pool when:

- It is an eligible transfer transaction.
- It has transfer category `1` or `2`.
- `ordpicked` is `F`.
- Its transaction status is `B`, `D`, or `E`.
- It belongs to the applicable subsidiary.
- It has at least one line with remaining work.

The open-line condition is more important than the order status. A packed order may remain in a NetSuite status that otherwise looks eligible, so the query must verify ordered quantity is still greater than packed or shipped quantity.

Status: **Enforced/tested**.

Source: `Integration.NS/NSScripts/NS_TO_x_Packing_Get_PendingFulfillment.sql:12-28`, `Integration.NS.Tests/Implementations/Transactions/Packing/PackingPendingFulfillmentFilterShapeTests.cs:111-160`.

### Packable quantity

For a transfer-order line, the remaining packable amount in display units is:

```text
max(0, ordered - max(packed, shipped) - backordered) / UoM rate
```

Additional rules:

- A nonpositive UoM rate is treated as `1`.
- Packed quantity cannot reopen a line already shipped.
- Backordered quantity is not packable.
- A fully backordered line allows no scan.
- Good and Bad scans together cannot exceed the packable amount.
- A line planned at zero is immediately treated as packed and is not scannable.

Status: **Enforced/tested**.

Source: `Shared.Libraries/ViewModel/TransferOrder/TransferOrderLineVM.cs:53-84`, `Integration.NS.Tests/ViewModels/TransferOrderLineVMPackableQuantityTests.cs:36-108`.

### Mobile vendor gate

- Mobile STR packing requires the operator to choose or scan a vendor belonging to the order.
- A vendor barcode is matched against the vendor name printed on the order.
- Matching ignores case, spacing, and punctuation according to the resolver tests.
- Vendors not on the order and ambiguous matches are rejected.
- Only lines belonging to the selected vendor are displayed for scanning.
- Changing vendors does not merge unsaved scans across vendors.
- The gate is mobile-specific. Web packing is not shown to apply the same gate.
- Null-vendor lines and server-side save revalidation remain unresolved.

Status: **Enforced/tested in mobile; unresolved as a universal rule**.

Source: `Mobile.MAUI/Components/Pages/Packing/TransferOrder/TransferOrderItemView.razor.cs:698-745`, `Integration.NS.Tests/Implementations/Transactions/Packing/TransferOrderVendorResolverTests.cs:53-96`, `.opencode/docs/changes/2026-10-21-mobile-str-vendor-gated-fulfillment.md:289-299`.

### Good and Bad scanning

- The scan-quality toggle is operator-controlled and cycles `Base -> Good -> Bad -> Base`.
- Scanning does not automatically change the selected quality.
- Base blocks ordinary scans on gated screens.
- Moving scans between Good and Bad requires enough quantity in the source bucket.
- Negating scans cannot subtract more than the amount already scanned.
- Comments about weight prompts and alternative scan paths are not active rules unless executable code also implements them.

Status: **Enforced**.

Source: `Mobile.MAUI/Components/Reusables/ScanToggleButton.razor:53-65`, `docs/mobile-good-bad-scan-toggle.md:38-71`, `Mobile.MAUI/Components/Pages/Packing/TransferOrder/TransferOrderItemView.razor.cs:1061-1128`.

### Fulfillment posting

- A fulfillment save requires a positive scan and one valid transfer-order ID.
- Duplicate line-and-quality rows are collapsed or rejected according to the save path.
- A bin-tracked location requires a valid bin for every positive scan.
- The server refreshes the authoritative open-line set before posting when reconciliation is enabled.
- A scan on a line that became closed or fully fulfilled is rejected.
- Open lines not being scanned are sent as zero-quantity fillers so the NetSuite transform does not fulfill them implicitly.
- Good and Bad quantities are posted as separate NetSuite calls, with Bad posted before Good.
- The two calls are not atomic. A successful Bad call cannot be rolled back if the Good call fails.
- Fresh-state read failure is fail-open by default and fail-closed only when the relevant configuration flag is enabled.

Status: **Enforced/tested in the primary TO path**.

Source: `Integration.NS/Services/NetSuiteApiClientService.cs:2553-2608,2618-2685,2761-2860`, `Integration.NS.Tests/Services/NetSuiteApiClientServiceTOFulfillmentReconciliationTests.cs:61-175`.

### Trip-ticket exemption and shipment status

- An exempt fulfillment is posted with ship status `C` immediately.
- A non-exempt fulfillment is posted with ship status `B` and waits for a trip ticket.
- Exemption lookup must resolve before posting; unknown exemption status is treated as an error rather than guessed.
- Packing labels are requested for both exempt and non-exempt fulfillments.

Status: **Enforced**.

Source: `Application.UseCases/Commands/Transaction/Packing/NS/TransferOrder/PostTransferOrderIFCmd.cs:23-49`, `.opencode/docs/WMS_PROCESS_FLOW.md:308-329`.

Known issue: the mobile save path filters Bad rows using `NSLineQuantityPacked != 0`, which may discard a valid Bad scan before the server receives it.

Source: `Mobile.MAUI/Components/Pages/Packing/TransferOrder/TransferOrderItemView.razor.cs:882-893`.

## Trip Tickets and Shipping

### Trip-ticket selection

The standard web candidate query selects Item Fulfillments when:

- The record type is `ItemShip`.
- The status is Packed (`B`).
- The fulfillment is not fully received: fully-received flag `F` or null.

Mobile trip-ticket creation:

- Scans fulfillment numbers, not item barcodes.
- Rejects duplicate fulfillment selections.
- Requires at least one selected fulfillment.

Web creation additionally requires date, destination, driver, helper, origin, and truck plate.

Status: **Enforced on the cited paths**.

Source: `Integration.NS/Implementations/Transactions/TripTicketIntegration.cs:73-103`, `Mobile.MAUI/Components/Pages/TripTicket/CreateTripTicketView.razor.cs:91-180`, `Web.BlazorServer/Components/Pages/Transaction/TripTicket/TripTicketCVU.razor.cs:548-592`.

Known issue: another packed-fulfillment SQL path also accepts a fulfillment with the print-label flag, so the “only status B” rule is not consistent across all selectors.

Source: `Integration.NS/NSScripts/NS_ItemFulfillment_Get_Packed.sql:15-18`, `docs/item-fulfillment-filtering.md:36-40`.

### Trip-ticket posting

- Selected fulfillments are patched to Shipped (`C`) before the trip-ticket record is posted or updated.
- Fulfillment updates run sequentially.
- There is no shown rollback if a later fulfillment update or trip-ticket write fails.
- Child trip tickets are not editable.
- A trip ticket can be cancelled only while its form status allows cancellation.
- Cancellation uses trip-ticket status `8`.

Status: **Enforced/documented**.

Source: `Integration.NS/Services/NetSuiteApiClientService.cs:3230-3336`, `.opencode/docs/WMS_PROCESS_FLOW.md:354-373`.

Known limitation: the repository does not show an anti-join preventing an already-ticketed fulfillment from appearing in every selection path.

## Receiving

### Web receipt preflight

A web item receipt requires:

- A positive source-order ID.
- For non-PO receipts, a positive shipped Item Fulfillment ID.
- At least one selected line with positive quantity.
- No negative line or allocation quantities.
- A source line number greater than zero.
- Quantity no greater than the line's open quantity.
- Inventory-detail quantities that sum exactly to the line quantity, including for non-bin items.
- Every positive allocation assigned status Good (`1`), Bad/Confiscated (`3`), or Missing (`6`).
- A valid bin for every positive allocation when the source is a PO and both the item and location are bin-controlled.
- No duplicate positive source line numbers.

Status: **Enforced/tested**.

Source: `Application.DataTransferObjects/Transactions/Receiving/ItemReceiptValidation.cs:8-48`, `Integration.NS.Tests/Implementations/Transactions/ReceivingPostingTests.cs:40-94`.

### Receipt posting order and failure behavior

- Receipt payloads are prepared before the first NetSuite write.
- Status buckets are posted in this order: Bad, Good, Missing.
- Posts are sequential, not concurrent.
- A failed bucket stops later buckets.
- Earlier successful buckets are reported so the operator can reconcile before retrying.
- NetSuite writes cannot be rolled back by the local transaction.
- A timeout can mean the ERP write succeeded even if the response looks like a failure.
- Successful posting means all planned receipt writes succeeded; it does not necessarily mean the source order is fully received.

Status: **Enforced/tested**.

Source: `Integration.NS/Implementations/Transactions/ReceivingIntegration.cs:870-955`, `docs/web-receiving-netsuite-status-fix.md:60-76,92-104,120-129`.

### PO receipt transforms

- Unscanned open PO lines are included as `itemreceive=false` fillers.
- Bin-tracked transforms retain an empty inventory-assignment object for fillers.
- Good and Bad receipt payloads must not contain each other's lines.
- A split source line appears once per payload, not as duplicate `orderLine` entries.
- A vendor-assigned bin takes precedence over a preferred bin.
- Bad received lines use zero rate in the relevant payload.

Status: **Enforced/tested**.

Source: `Integration.NS.Tests/Payloads/PurchaseOrderIRPayloadDTOTests.cs:87-168,233-385`.

### Non-PO receipt freshness

- Before posting a non-PO web receipt, the system re-reads the transfer-order ledger.
- An allocation above the current remaining quantity is rejected.
- This protects against TO-to-IR receipts that are not visible through fulfillment-based quantity calculations.
- The read and subsequent ERP writes are still not one atomic operation.

Status: **Enforced; test coverage not identified**.

Source: `Integration.NS/Implementations/Transactions/ReceivingIntegration.cs:816-868`.

### Returns receiving

- An operator may confirm that the unscanned remainder should be marked Missing.
- If the operator cancels that confirmation, only scanned quantities are posted and the remainder stays open.
- An unrecognised transfer category must not silently determine fulfillment status.
- If no positive quantity is scanned, no fulfillment is created.
- If trip-ticket exemption cannot be resolved, the return save fails instead of guessing a ship status.

Status: **Enforced**.

Source: `Mobile.MAUI/Components/Pages/Receiving/Returns/TOxReturnxItemFulfillmentItemView.razor.cs:503-622`, `Application.UseCases/Commands/Transaction/Packing/NS/Returns/PostReturnsIFCmd.cs:24-78`.

## Returns and Vendor Returns

- Supplier-return lines require their own Preferred Vendor.
- New supplier-return lines default to the header vendor, or to the item's preferred vendor when no header vendor exists.
- Header vendor values are cloned per line so editing one line does not change other lines.
- SAP goods returns send only positive-quantity lines.
- GRR-based, GRPO-based, and standalone returns use different payload/base types.

Status: **Enforced on the cited paths; SAP rules are legacy**.

Source: `Web.BlazorServer/Components/Pages/Transaction/SupplierReturn/Components/SupplierReturnForm.razor.cs:271-329`, `Integration.SAP/Implementations/Transaction/GoodsReturn/GoodsReturnIntegration.cs:127-191`.

## Inventory Counting

### Physical-count rules

- A physical count may exceed the planned or system quantity.
- A count may be positive even when the plan is zero.
- Bad stock is physically present and contributes to total counted quantity.
- Total counted quantity is Good plus Bad.
- An explicitly counted zero is sent to NetSuite.
- Untouched lines are not automatically zeroed.

Status: **Enforced/tested**.

Source: `.opencode/docs/changes/2026-10-20-inventory-counting-manual-planned-qty.md:37-44,60-90`, `Application.DataTransferObjects/Transactions/InventoryCounting/NS/Payload/PatchInventoryCountingPayloadDTO.cs:11-69`.

### Local count document lifecycle

- A new document starts in `Open`.
- Saving requires `Open` and moves the document to `Saved`.
- Recounting requires `Saved` and moves it to `Recount`.
- Posting requires `Saved` and successful ERP posting before moving to `Posted`.
- A duplicate document for the same warehouse, cycle, and period is rejected by the application check.
- The duplicate check does not by itself prove a database uniqueness constraint or eliminate concurrent creation races.

Status: **Command-enforced**.

Source: `Application.UseCases/Commands/Transaction/InventoryCounting/CreateInventoryCountingDocumentCmd.cs:28-65`, `SaveInventoryCountingDocumentCmd.cs:17-25`, `RecountInventoryCountingDocumentCmd.cs:17-25`, `PostInventoryCountingDocumentCmd.cs:20-40`.

Known limitations:

- The documented NetSuite posting implementation is currently unimplemented.
- Adding a sheet marks it Synced immediately, which can make the normal Async-to-Synced command path unreachable for UI-created sheets.
- The sheet-sync command checks sheet status but does not visibly check the parent document status.

Source: `.opencode/docs/WMS_PROCESS_FLOW.md:564-579`, `Domain.Entities/Entities/Transaction/InventoryCounting/InventoryCountingDocumentDEM.cs:65-80`.

## Inventory Worksheet and Adjustment

### Inventory Worksheet

An inventory worksheet requires:

- A selected location.
- At least one positive line.
- No negative quantities.
- Allocated quantity exactly equal to total quantity.
- A bin when the selected location uses bins.

Status: **Documented validation**.

Source: `.opencode/docs/WMS_PROCESS_FLOW.md:581-593`.

### Inventory Adjustment

- A document cannot mix issue lines and receipt lines.
- Receipt lines have positive received quantity and no positive issued quantity.
- Issue lines have positive issued quantity and no positive received quantity.
- NetSuite quantity is calculated from allotted quantity multiplied by the UoM conversion rate.

Status: **Enforced/documented**.

Source: `.opencode/docs/WMS_PROCESS_FLOW.md:489-512`.

## SAP Legacy Rules

The following rules apply to the inspected SAP integrations and should not automatically be treated as rules for the active NetSuite workflows:

- SAP delivery payloads include only positive-quantity lines.
- SAP purchase delivery-note payloads include only positive-quantity lines.
- Positive-price lines use a PO-based receipt line when a base entry exists.
- Nonpositive-price lines use standalone payload lines.
- SAP goods receipts and goods returns include only positive-quantity lines.
- SAP inventory transfers include only positive-allotment lines.
- A particular SAP `-2028` inventory-transfer response is treated as a created approval draft only when a parseable `Location: Drafts(id)` header exists.

Status: **Legacy/enforced in SAP code**.

Source: `Integration.SAP/Implementations/Transaction/Delivery/DeliveryIntegration.cs:124-140`, `Integration.SAP/Implementations/Transaction/Receiving/ReceivingIntegration.cs:131-154`, `Integration.SAP/Implementations/Transaction/GoodsReceipt/GoodsReceiptIntegration.cs:130-164`, `Integration.SAP/Implementations/Transaction/GoodsReturn/GoodsReturnIntegration.cs:127-191`, `Integration.SAP/Implementations/Transaction/InventoryTransfer/InventoryTransferIntegration.cs:233-305`.

## Known Documentation Conflicts

These items should be resolved before this catalogue is treated as the final policy source:

1. The process reference describes concurrent web receipt posts, while current code and tests use sequential Bad, Good, Missing posts. Current code/tests are the stronger evidence.
2. The process reference describes Missing as mobile-only, while current web validation and posting support Missing.
3. The process reference previously described the STR category whitelist as only Store Supplies (`5`); current shared code uses `4,5,9`.
4. One trip-ticket selector requires status `B`, while another accepts a print-label condition that may include status `C`.
5. Mobile vendor selection is not equivalent to server-side vendor validation and does not cover Web packing.
6. The mobile TO packing comment says rows are forwarded unchanged, but the Bad-row filter can remove rows before the server receives them.
7. Local inventory-counting documents expose a lifecycle that includes posting, while the documented NetSuite post implementation is unimplemented.
8. Several change-request and RCA documents describe proposed or recommended behavior. They must not be treated as deployed rules without code or test confirmation.

## Maintenance Rules for This Catalogue

When changing business logic:

- Update the relevant section in this document.
- Add or update an automated test for the rule when practical.
- Identify whether the rule is enforced at the UI, application, domain, database-query, or ERP boundary.
- Link the implementation and test files.
- Mark the rule as legacy or unresolved instead of deleting historical context.
- When documentation conflicts with code, record the conflict and state which source is currently trusted.
- Do not describe a UI filter as a security or authorization boundary unless the write operation revalidates the rule.
