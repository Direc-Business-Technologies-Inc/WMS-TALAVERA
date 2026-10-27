# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https:-/keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Fixed
- (Receiving → PO Item Receipt) Added fresh NetSuite quantity validation before
  posting and mobile revalidation after Refresh, manual entry, and before Save.
  The API now validates combined GOOD + BAD quantities per PO line, rejects
  stale/mismatched line state before either receipt post, and preserves the
  existing open-line filler and bin-detail payload behavior. Posting failures
  advise checking NetSuite receipts before retrying because a response failure
  does not prove that a receipt was not created.
- Spec: `.github/specs/2026-10-27-po-item-receipt-fresh-quantity-validation.md`.

## [0.12.14] - 2026-10-20
### Fixed
- (Receiving / Packing / Counting → Mobile) "Unknown barcode" still appeared for
  correct barcodes after 0.12.9 fixed only the five Receiving views. Two defects
  remained:
  1. **Four screens never got the fallback.** Packing TO, Packing Returns,
     Packing VRA and Inventory Counting still matched a scan against the
     per-document `/Item/Barcodes` list only, so a stale, empty or failed list
     produced the warning on every scan.
  2. **A failed lookup was reported as an unknown barcode.** `ResolveBarcodeAsync`
     checked only `res.Data is null`, which is also what a 4xx/5xx/timeout
     yields. A NetSuite error therefore rendered as `Unknown barcode: …`, hiding
     the real cause — the same class of masking 0.12.9 removed at the list layer.
- `POST /Item/PerBarcode` is now the single source of truth for scan resolution:
  - Mobile: one `BarcodeResolver` (`Mobile.MAUI/Services/BarcodeResolver.cs`)
    replaces eight copies of the resolver and the per-document barcode list
    (`ActionGetItemBarcodes` / `ItemRequest` / `ItemBarcodes`) on all nine scan
    screens. It normalises the scan token (trim, AIM `]C1` prefix, control
    characters a keyboard-wedge appends), caches hits per barcode for the
    session, resets the cache when the endpoint changes, and returns one of
    three outcomes: found, not-found, or failed-with-reason. Screens now show
    `Barcode lookup failed: <reason>` for the last case instead of
    `Unknown barcode`.
  - API: `GET`-equivalent handler returns `InventoryItemDTO?`
    (`FirstOrDefault` instead of adapting an `IEnumerable` to a single VM, which
    produced a default object), and the controller answers `NotFound`
    (`StatusCode 404` in the body) for "no such barcode" while real errors keep
    their own status. Blank scans are rejected as `Failed`.
  - `NS_Item_Get_PerBarcode.sql`: dropped the `itemtype = 'InvtPart'` filter,
    which `NS_Item_Get_Barcodes.sql` never had — it silently made every
    non-`InvtPart` line unresolvable. Added `ORDER BY i.id` for a deterministic
    pick when a barcode is duplicated in the custom record.
- `ItemBarcodesPerUoMVM` no longer re-declares `UoMName`, `UoMRate` and
  `DefaultWeight`. The shadowed members meant the Inventory Worksheet read
  `""`/`0` after a scan (its `ProcessScannedItem` takes `InventoryItemVM`).
- (Packing / Receiving → Mobile) The "Last Scanned" panel no longer sits
  permanently on top of the item list. It is now driven by the `#recent-scanned`
  sentinel with an `IntersectionObserver` (`threshold: 0`, `isIntersecting`):
  hidden while the operator is at the top of the list, visible once they scroll
  into it, and hidden again when they return to the top. A scan always reveals
  it, and when that scan happens at the top the panel auto-hides after ~3s.
  Restores the intent of `06a3cc7` without its two defects — it read
  `window.scrollY` (wrong for screens that scroll an inner container) and cached
  the panel node in an ES module that outlives the page, so the panel went dead
  after navigation. The hidden state is now `translate(-50%, -100%)` instead of
  a `-170px` offset that leaked a strip when a material name wrapped.
  Behaviour reference: `last-scanned-panel-fix-summary.md`; decision log:
  `.opencode/docs/changes/2026-10-20-last-scanned-panel-scroll-behaviour.md`.

### Added
- `NSScriptsWellFormedTests.Item_per_barcode_script_matches_the_barcode_list_scope`
  pins the two barcode scripts to the same item-type scope.

### Operational
- Requires **both** an API deploy and a new mobile APK. The API must ship first:
  an old API has no `NotFound` result, so the client would report "not found"
  as an ordinary failure.
- Live smoke test (one per screen family): scan a valid barcode with the network
  cut mid-scan (expect `Barcode lookup failed: …`, not "Unknown barcode");
  scan a barcode that NetSuite does not know (expect `Unknown barcode: …`); scan
  one that is valid but absent from the document (expect `Item not found in this
  PO/…`); repeat the same barcode to confirm the second scan is served from the
  cache.
- Verify `POST /api/Item/PerBarcode` with a known barcode returns a single object
  with the real `NetsuiteMaterialInternalId` — the previous `Adapt` over an
  `IEnumerable` is the reason this was never trustworthy.
- `dotnet build Api.CoreWebAPI` and `dotnet build Mobile.MAUI -f
  net9.0-windows10.0.19041.0` → 0 errors. `dotnet test Integration.NS.Tests` →
  476 pass; the two documented timing-sensitive tests
  (`GridResultCacheTests.Key_index_does_not_outlive_the_cache_it_tracks`,
  `NetSuiteApiClientServiceTokenTests.HttpClient_has_explicit_timeout_of_100_seconds_by_default`)
  fail only in the full run and pass in isolation.
- `/Item/Barcodes` remains for the web pages (`InventoryCountingHandler`) only.

---

## [0.12.13] - 2026-10-19
### Fixed
- (Item Selection → STR / ITR / Supplier Return / Inventory Adjustment) The item
  picker's search box accepted input but the grid never narrowed — it returned
  the same rows as the unfiltered list. The search builds an OR-group of
  `ItemNumber` / `Name` / `Description` `Contains` filters with
  `LogicalOperator = OR` and **no `Property` of its own**, and
  `SuiteQLQueryBuilder._parseFilter` resolved a filter's own `Property` *before*
  checking whether it was a group. The empty group property resolved to nothing,
  so the whole group was dropped (log: `Dropped grid filter on unmapped property
  ''`) and its children were never parsed; the effective `WHERE` equaled the
  unfiltered load's.
- `_parseFilter` now resolves a group (a container with only children) before its
  own `Property`, so an empty/absent group property can no longer discard it.
  `_parseFilterGroup` also drops unresolved children before joining, so a
  partially-resolved group can no longer emit malformed SQL such as `( OR … )`;
  an all-dropped group collapses to null like any other dropped filter.
- Ruled out during the investigation: the grid-result cache (its key serializes
  the full `DataGridIntent`, search term included, so a new term is a cache miss)
  and the input event wiring (`RadzenTextBox` 9.0.8 splats `@oninput` onto the
  `<input>` when `Immediate=false`).

### Added
- `SuiteQLUnresolvedPropertyGuardTests` +2 cases: an OR-group with no `Property`
  still applies its children; a group mixing a resolvable and an unresolvable
  child keeps the resolvable predicate and emits no malformed SQL.

### Operational
- Server/query-builder only: deploys with the API; no mobile rollout needed (the
  mobile search boxes build their own filters and were never affected).
- Verified: `dotnet build Integration.NS` → 0 errors; filter/query test sweep →
  109/109 passed.
- **Client-confirmed 2026-10-19 (web):** searching an impossible term returns
  "No records", a real term narrows the grid, and the `Dropped grid filter on
  unmapped property ''` warning no longer logs.
- Spec: `.github/specs/2026-10-19-item-search-or-group-filter-dropped-fix.md`.

---

## [0.12.12] - 2026-10-19
### Fixed
- (Receiving → PO Item Receipt) A **partially scanned** purchase order at a
  bin-tracked location failed the last step with `[INVALID_PARAMETER] Resource
  or sub-resource 'InventoryDetail' from the query parameter replace was not
  found in the request body` (`[diag replace=True usedBin=True scannedLines=31
  totalLines=52]`). This is the same failure the 2026-10-15 spec traced on the
  `itemFulfillment` transform, now reproduced on `itemReceipt`.
- **Root cause — the `?replace=` fix and the "post every open line" mobile
  change were incoherent together.** On a bin-tracked location the URL declares
  `replace=item.inventoryDetail.inventoryAssignment`, so NetSuite requires that
  sub-resource on **every** entry in `item.items[]`. The mobile posts all open
  lines (Req 2.1), the unscanned ones as zero-qty fillers — and
  `PurchaseOrderIRPayloadDTO` emitted those fillers as
  `{orderLine, itemreceive:false}` with **no** `inventoryDetail`. The body
  carried the sub-resource on 31/52 entries and NetSuite rejected the whole
  transform. A fully scanned order (no fillers) still passed, which is why the
  failure looked intermittent.
- `PurchaseOrderIRPayloadDTO.CreateForItemReceipt` now takes `isUsedBin` and,
  when the location is bin-tracked, gives fillers an **empty assignment**
  (`inventoryAssignment.items: []`) — the same field-proven shape the
  fulfillment builders emit. `SavePOItemReceipt` passes the same
  `Data.Any(x => x.IsLocationUsedBin)` flag that builds the `?replace=` URL, so
  body and URL cannot drift. Non-bin PO posts are unchanged.
- The **same latent defect** existed in the Returns Item Receipt builder:
  `SaveReturnsItemReceipt` always posts with an unconditional `?replace=`, and
  `ReturnsIRPayloadDTO` emitted detail-free fillers. Returns fillers now carry
  the empty assignment unconditionally (a partial returns scan would have
  failed identically). No signature change.
- Corrected the 2026-10-19 PO line-fan-out spec, which had chosen to leave PO
  fillers detail-free on the (now refuted) assumption that the `itemReceipt`
  transform tolerates a detail-free filler.

### Added
- `PurchaseOrderIRPayloadDTOTests.Bin_tracked_orders_cover_every_entry_with_inventory_detail`
  (theory, GOOD + BAD): a partially scanned bin-tracked order emits
  `inventoryDetail` on every entry — the scanned line and the empty-assignment
  filler.
- `PurchaseOrderIRPayloadDTOTests.Non_bin_orders_leave_filler_lines_without_inventory_detail`:
  the non-bin filler shape is unchanged.

### Operational
- **Client-confirmed working on 2026-10-19:** the partially scanned bin-tracked PO that
  failed with the INVALID_PARAMETER replace error now posts. Server-side only: the API
  redeploy was sufficient, **no mobile rollout needed** (the APK already posted every
  open line — `totalLines=52 > scannedLines=31` in the failing diag).
- The Returns Item Receipt filler (same remedy, unconditional) ships in source; a partial
  returns scan has not yet been exercised, so treat Returns parity as unverified until it is.
- Spec: `.github/specs/2026-10-19-po-item-receipt-filler-inventory-detail-fix.md`.

---

## [0.12.11] - 2026-10-19
### Fixed
- (Packing → STR, Web Item Receipt) Fulfilling a **partially fulfilled**
  transfer order from the web STR Item Receipt page failed the last step with
  `Error while posting GOOD TO Item Fulfillment` →
  `Please configure the inventory detail for <item>`
  (`[diag replace=True usedBin=True scannedLines=3 totalLines=561]`). The web
  handler populated neither `IsClosed` nor `QuantityAlreadyShipped`, so the
  server guard (`IsClosedFulfillmentLine` / `FindScansExceedingRemaining`) was
  inert on the web path; the web pre-filter also used the packed-based
  remaining, so closed lines survived and were emitted as `orderLine`
  references and zero-qty fillers for lines the transform no longer contains.
- Remaining is now `LineQuantity − QuantityAlreadyShipped`, matching the two
  existing authorities (mobile `TransferOrderLineVM.NSLineQuantityRemaining`
  and `PostTransferOrderDTO.RemainingQuantity`), and closed / fully-shipped
  lines are excluded before mapping via `IsCompleted`. `IsClosed` and
  `QuantityAlreadyShipped` are carried onto the DTO, re-arming the server
  guard as defence-in-depth for stale clients and manually closed lines.
- `DecodeTOItemFulfillmentLine` now resolves both NetSuite phrasings — `line N
  of the item list` and `inventory detail for <item>.` — in the GOOD and BAD
  TO catch blocks, so a failure names the offending item even when the message
  carries no line index.

### Added
- `NetSuiteApiClientServiceTOFulfillmentDiagnosticsTests` (5 cases): both
  phrasings, a dotted item name (`DEL MONTE P/A SLICED 1.5TINS 432G`),
  resolution by `MaterialCode`, unmatched-name reporting, and a message with
  neither marker.

### Operational
- **Client-confirmed successful on 2026-10-19:** the originally failing
  partially fulfilled TO now saves from the web STR Item Receipt page, and no
  `orderLine` for a closed line survives into the GOOD post.
- Handler/server-side only: deploys with the API; no mobile rollout is needed.
  **Not separately exercised:** an ordinary open TO at a bin-tracked location,
  and a TO with `UoMRate < 1` (the now-armed over-scan guard compares
  display-UoM scanned quantity against stock-UoM remaining — the same
  assumption the mobile path has used since 2026-10-16).
- Spec: `.github/specs/2026-10-19-web-str-packing-closed-line-guard-missing-fix.md`.

---

## [0.12.10] - 2026-10-19
### Fixed
- (Receiving → PO Item Receipt) An open purchase order at a bin-tracked
  location failed the last step with `Please configure the inventory detail in
  line 1 of the item list` (`[diag replace=True usedBin=True scannedLines=1
  totalLines=1]`, and again with `scannedLines=20 totalLines=20`). This is the
  same NetSuite text as the two 2026-10-17 PO fixes but a third route to it:
  the PO line query, the mobile's line selection, and a missing pre-flight
  guard.
- **Root cause — the PO line query fanned a line out and could resolve the
  wrong bin.** `NS_PurchaseOrder_Get_Items.sql` joined
  `customrecord_dbti_vendor_bin_assignment` on the vendor alone (no item,
  subsidiary or location, no grouping), so a vendor with N bin-assignment
  records multiplied every PO line into N rows and could return a bin
  belonging to a different item/location. The preferred-bin subquery was
  ungrouped too. Both now GROUP BY and take `MIN`, mirroring the transfer-order
  queries (`NS_TO_Get_Itemfulfillment_Items.sql`,
  `NS_TO_x_Packing_Get_Items.sql`); the vendor subquery is scoped through
  `itemvendor` (`preferredvendor = 'T'`) by item, subsidiary and location.
- The mobile `PurchaseOrderItemView.SaveScan` posted only the scanned lines, so
  the payload's item sublist was a partial subset of the transform's open
  lines. It now posts every open line, sending the unscanned ones as
  detail-free fillers (`isReceived: false`) — the shape the production Returns
  receipt uses (Req 2.1 of the 2026-10-08 mobile PO line-selection spec). The
  itemReceipt transform tolerates these fillers, so the transfer-order
  empty-assignment shape was deliberately **not** ported.
- `PurchaseOrderIRPayloadDTO.CreateForItemReceipt` now prefers the scanned row
  when a line still repeats, instead of trusting query row order (the same
  tie-break `DeduplicateFulfillmentLines` applies).
- `SavePOItemReceipt` gained the missing-bin pre-flight guard the transfer-order
  path already had: at a bin-tracked location a scanned line with no resolvable
  vendor or preferred bin now fails before the NetSuite call, naming the WMS
  line, material and location, rather than surfacing the cryptic 400. It judges
  each line on its own `IsLocationUsedBin` (a PO can mix locations) and each
  good/bad bucket separately (the two posts are separate NetSuite calls).
- **Confirmed business rules (2026-10-19):** the vendor bin is resolved from the
  item's preferred vendor regardless of which vendor the PO was raised against,
  and when no vendor bin exists at the location the line falls back to the
  item's preferred bin. The SQL and payload therefore stay as described above.
- **Diagnostics.** Both PO catch blocks now emit `build={BuildStamp}` and a
  decoded `lineN=WMSLine … usedBin=… vendorBin=… preferredBin=… qty=…` segment
  via the new `DecodeItemReceiptLine`, which indexes the same sublist the
  payload builder emits. The counters fingerprint the mobile build only in one
  direction: `totalLines > scannedLines` proves the payload carries unscanned
  fillers, but equality is inconclusive (a fully-scanned order reports equal on
  either build).

### Operational
- The SQL and server changes deploy by redeploying the API; the mobile
  change (post every open line) needs an APK rollout. The counters fingerprint
  the build in one direction only: `totalLines > scannedLines` proves the
  payload carries unscanned fillers, but equality is inconclusive (a
  fully-scanned order reports equal on either build).
- **Client-confirmed successful on 2026-10-19 (API and APK deployed):** the
  previously failing open PO at a bin-tracked location now posts and the mobile
  Item Receipt completes. Where a guard fires, the diagnostic `lineN=…` segment
  names the offending WMS line, material and location.
- Verified to compile: `dotnet build Integration.NS` and `dotnet build
  Mobile.MAUI/Mobile.MAUI.csproj -f net9.0-android` → 0 errors. Filtered PO test
  run → 53/53 passed.
- **Not separately exercised:** a vendor with several bin assignments / an item
  with several preferred bins (expected: one line per PO line), and a
  mixed-location PO (its non-bin lines must not be blocked by the guard).
- Spec: `.github/specs/2026-10-19-po-item-receipt-line-fanout-and-fillers-fix.md`.

---

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
