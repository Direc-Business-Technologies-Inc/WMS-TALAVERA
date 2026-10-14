# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https:-/keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
