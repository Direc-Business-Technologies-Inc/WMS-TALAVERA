# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https:-/keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.12.9] - 2026-10-17
### Fixed
- (Receiving → Mobile) Operators received a false "Unknown barcode" toast while
  scanning on the mobile receiving screens even though the item's barcode was
  valid. The mobile item views matched a scan only against a per-document
  barcode list loaded once through `POST /Item/Barcodes`, and warned on the
  first miss. Three conditions that have nothing to do with the barcode itself
  produced the warning: (1) the list had not loaded yet — the view subscribes to
  the scan broadcast in `OnInitializedAsync` but loads the list in
  `OnAfterRenderAsync`, so an early scan searches an empty list; (2) the list
  fetch failed — `ActionGetItemBarcodes` defined no `OnFailure` and
  `ActionFactoryService.ExecuteAppActionAsync` swallows the exception, leaving
  `ItemBarcodes` empty so *every* later scan warned for the rest of the session;
  (3) the item was absent from a stale or partial list.
- Scans are now resolved through a `ResolveBarcodeAsync` helper instead of a
  direct list lookup: the cached list first; when the list is empty it reloads
  once (covers 1 and 2); when the scan is still missing it calls the existing
  authoritative `POST /Item/PerBarcode` endpoint (covers 2 and 3); only when
  NetSuite also knows no such barcode does the view warn. A fallback hit is
  synthesized into a barcode row (`MaterialBarcode = scanned`), so the existing
  good/bad line selection and over-scan guards are unchanged.
- `ActionGetItemBarcodes` now defines `OnFailure`, so a failed list load
  surfaces `Failed to load item barcodes: …` instead of silently degrading into
  false unknowns. Applied to all five receiving views
  (`PurchaseOrderItemView`, `TransferOrderItemView`,
  `TOxItemFulfillmentItemView`, `ReturnsItemView`,
  `TOxReturnxItemFulfillmentItemView`) — fifteen scan sites in total.

### Operational
- Client-side only: the fix ships with a new mobile build (APK). No API code
  change is required — the fallback reuses `POST /api/Item/PerBarcode`
  (`ItemController`). Redeploy the API only if the live build predates that
  endpoint; confirm by checking that mobile Inventory Worksheet scanning works,
  since it already calls the same endpoint.
- Verified to compile: `dotnet build Mobile.MAUI/Mobile.MAUI.csproj -f
  net9.0-windows10.0.19041.0` → 0 errors. Not yet verified on a device.
- Smoke test PO and TO receiving: scan a valid barcode while the list is still
  loading (expect resolution, no warning), and force a `/Item/Barcodes` failure
  (expect the "Failed to load item barcodes" toast rather than an "Unknown
  barcode" on every scan).
- A barcode NetSuite genuinely does not know — no `customrecord_barcode_per_uom`
  row for the item — still warns, which is the intended behavior.

---

## [0.12.8] - 2026-10-17
### Fixed
- (Receiving → PO Item Receipt) A purchase order that was already fully
  received could still be submitted for receipt. `SavePOItemReceipt` had no
  closed-line guard, unlike the transfer-order fulfillment path
  (`IsClosedFulfillmentLine` / `FindScansExceedingRemaining`). A fully received
  PO line is absent from the `!transform/itemReceipt` item sublist, so the
  scanned entry — or the zero-qty filler the payload builder emits — for it is
  an invalid insert on a static sublist, and NetSuite rejects the entire save
  with "Please configure the inventory detail in line N". Field case: PO
  `170552` (`PO-THM00100000216-TR`), all 18 material lines `qty == recvd`, with
  three receipts already posted (`IR-THM00100000951/956/959-TR`).
- The new guard keys on remaining open quantity
  (`LineQuantity − LineQuantityReceived`), **not** the line's `isclosed` flag:
  a fully received PO line still reports `isclosed = F` (verified via SuiteQL on
  the failing PO), so a flag-based predicate would let the post through.
  Remaining is converted to the operator's display UoM via `UoMRate` before it
  is compared against `ScannedQuantity`.
- Closed lines are stripped before `isUsedBin` and the `?replace=` URL are
  derived, so a bin-tracked closed line can no longer select the replace URL for
  a save that will not happen. When every scanned line is already received, the
  save fails with an actionable message naming the offending lines instead of a
  cryptic NetSuite 400.

### Added
- `NetSuiteApiClientServicePOClosedLineGuardTests` (16 cases) pins the
  remaining-based predicate, the over-scan descriptor, the `UoMRate` scaling and
  the `isclosed = F` regression.

### Operational
- Not yet verified against live NetSuite. Do **not** re-receive PO `170552` — it
  is fully received. Smoke test a genuinely open PO and a partially received PO
  at a bin-tracked location, and confirm a scan beyond the remaining quantity
  fails with the WMS-line message rather than a NetSuite 400.
- The fix is server-side and needs no mobile rollout. The mobile PO screen's own
  remaining-quantity gate (added 2026-06-05) validates against the values loaded
  when the document was opened, so a screen left open while the PO is received
  elsewhere can still submit closed lines — now caught here instead of at
  NetSuite.

---

## [0.12.7] - 2026-10-16
### Fixed
- (Packing → STR, Returns, VRA) Orders that were already fully packed still
  appeared on the pending-fulfillment list with all of their lines, and
  operators packed them a second time. The lists filtered on order state only
  — `ordpicked = 'F'` and `status IN ('B','D','E')` — but `ordpicked` tracks
  *picking*, not *fulfilling*, and the transfer-order status only leaves that
  set once the Trip Ticket ships the item fulfillment. The WMS posts packing
  fulfillments with shipStatus `B` (Packed, not Shipped), so between packing
  and shipping a fully packed order still matched.
  Added a quantity-based gate — `SuiteQLFragments.PendingPackingLines`, a
  derived table of transactions with at least one open line whose ordered
  quantity still exceeds its packed quantity, inner-joined on the transaction
  id — to all three packing lists
  (`StockTransferRequestPackingIntegration`, `ReturnPackingIntegration`,
  `VendorReturnAuthorizationPackingIntegration`, list and header lookups) and
  mirrored it in the three mobile scripts
  (`NS_TO_x_Packing_Get_PendingFulfillment`,
  `NS_TO_x_Return_x_Packing_Get_PendingFulfillment`,
  `NS_VendorReturnAuthorization_Get_PendingReturn`) so the lists stay in
  parity. Partially packed orders remain listed, with correct remaining
  quantities.
- Comparisons use `ABS` on both sides of the remaining-quantity test, because
  transfer-order and VRA lines carry negative quantities in the return
  direction. VRA compares against `quantityshiprecv`, the column its packing
  line script already reads; the transfer-order lists compare against
  `quantitypacked`.
- The gate is a join, not an `EXISTS (... = t.id)`: correlation from a
  subquery into the outer query's aliases is rejected by NetSuite with
  400 INVALID_PARAMETER (Rule 1 of `.opencode/docs/SuiteQL-Shape-Rules.md`).

### Added
- `PackingPendingFulfillmentFilterShapeTests` (26 cases) locks the gate's
  shape in all three lists and all three mobile scripts: the derived-table
  join, the ABS comparisons, the closed/mainline predicates, the per-flow
  packed-quantity column, the absence of outer-alias correlation, and
  continued structural well-formedness.

### Operational
- Not yet verified against live NetSuite. Smoke test a fully packed order on
  each of the three tabs — web and mobile — and confirm it drops off after a
  refresh while a partially packed order stays.
- NetSuite's `isclosed` on a transfer-order item line must be `'F'` for lines
  with quantity still to pack. If NetSuite closes a line at pack time instead,
  the gate would drop partially packed orders; the scripts and the C# filter
  must then be re-pointed.
- Packing itself is unchanged — the post-time guards (`IsClosedFulfillmentLine`,
  `FindScansExceedingRemaining`) still stand. This fix only stops the order
  being offered.

---

## [0.12.6] - 2026-10-12
### Fixed
- (Packing → Returns) Returns that had already been packed were still listed on
  the tab. The Returns filter was missing the `ordpicked = 'F'` guard that the
  STR tab already applies, so it had no equivalent of the "not yet picked"
  check. Added to both the Web SuiteQL filter
  (`ReturnPackingIntegration.PackingReturnsFilters`) and the Mobile script
  (`NS_TO_x_Return_x_Packing_Get_PendingFulfillment`) so the two lists stay in
  parity.

### Operational
- Not yet verified against live NetSuite. Smoke test with a return that has
  already been packed and confirm it drops off the tab after a refresh.
- Confirm NetSuite sets `ordpicked = 'T'` when a Return Item Fulfillment posts.
  If it does not, this guard will not remove packed returns, and a
  quantity-based filter (`quantitypacked < quantity`) will be required instead.
- Status `E` remains in both the packing filters (`B`, `D`, `E`) and the
  receiving filters (`F`, `E`). Its meaning in `transferorderstatus` is still
  unresolved, so returns sitting at `E` that have not been picked will still
  appear.

---

## [0.12.5] - 2026-10-10
### Added
- (Stock Transfer Request, Returns to Supplier) Per-row Print button on the
  STR grid (all three tabs) and the Returns to Supplier grid, alongside the
  existing row action
- `NetSuitePrint` centralizes NetSuite printout URL construction and
  `AppPrintLink` replaces the copy-pasted print button markup

### Fixed
- STR grid rows were never selecting `t.id`, so `StockTransferRequestDataGridVM.Id`
  silently deserialized as `0` on every row. Required for the printout URL.
- Returns to Supplier grid did not surface the NetSuite internal id at any layer
- Removed two byte-identical transfer order printout constants and the dead
  ternary that selected between them

### Operational
- STR and Returns to Supplier grids are cached for 60s. Clear the grid cache on
  release so the new print buttons are immediately usable.
- Not yet verified against live NetSuite. Smoke test all three STR tabs plus
  Returns to Supplier; the Returns tab runs live paged SuiteQL.

---

## [0.12.4] - 2026-10-09
### Added
- (Stock Transfer Request) Requested By header field for TO, ITO and Return —
  sourced from NetSuite employees, required on save, persisted to the standard
  NetSuite `employee` field, and shown as a new filterable/sortable list column
  on all three STR tabs

### Operational
- STR grid results are cached for 60s (`CachedStockTransferRequestIntegration`).
  Clear the grid cache on release so the new Requested By column is populated
  immediately.

---

## [0.12.3] - 2026-09-17
### Fixed and Added
- (Trip Ticket) IF Selection and UI adjustments
- Adding of Semaphore in NetsuiteApiClientService
- Removing of Manual in Packing and Adjust the PO process of Missing

---

## [0.12.2] - 2026-08-27
### Added
- Add Vendor and Document Type in ItemReceipt Datagrid

---

## [0.12.1] - 2026-08-27
### Added
- Add Search Bar in Item Selection

---

## [0.12.0] - 2026-08-26
### Added
- Manual Entry for Packing and Receiving (Mobile)

---

## [0.11.0] - 2026-08-26
### Added
- Receiving by Weight Mode (Mobile)

---

## [0.10.2] - 2026-08-26
### Fixed
- include missing status in posting (Web)

---

## [0.10.1] - 2026-08-26
### Fixed
- Show Purchase Category in STR and InterCompany

---

## [0.10.0] - 2026-08-26
### Changed
- Trip Ticket Adjustment
	- Adding of parent where the user can select again a trip ticket

---

## [0.9.0] - 2026-08-04
### Fixed
- UAT Adjustments

---

## [0.8.1] - 2026-04-07
### Added
- Inventory Counting Bar Code Scanning
- Receiving Bar Code Scanning

---

## [0.8.0] - 2026-04-06
### Added
- Inventory Counting *(view, create, update, arhive users)*

---

## [0.7.0] - 2026-03-16
### Added
- Inventory Transfer *(view, create, update, arhive users)*

---

## [0.6.0] - 2026-03-16
### Added
- Sales Return *(view, create, update, arhive users)*

---

## [0.5.0] - 2026-03-12
### Added
- Sales Delivery *(view, create, update, arhive users)*

---

## [0.4.1] - 2026-03-10
### Added
- Goods Receipt PO *(additional items posting)*
- Goods Return *(goods return requests posting)*
- Goods Return *(copy from grpo posting)*
- Additional UDFs *(goods receipt po, goods return, goods issue, goods receipt)*

### Fixed
- Goods Receipt PO posting
- Goods Receipt posting
- Goods Issue posting
- Standalone Goods Receipt posting
- User Mangement Form UI

---

## [0.4.0] - 2026-02-24
### Added
- Goods Issue *(view, post documents)*
- Goods Receipt *(view, post documents)*

---

## [0.3.0] - 2026-02-24
### Added
- Goods Return *(view, post documents)*

---

## [0.2.0] - 2026-02-10
### Added
- Goods Receipt PO *(view, post documents)*

---

## [0.1.0] - 2026-02-12
- Initial Project Setup

### Added
- User Management *(view, create, update, arhive users)*
- Role  Management *(view, create, update, arhive roles)*
- User Authorization
