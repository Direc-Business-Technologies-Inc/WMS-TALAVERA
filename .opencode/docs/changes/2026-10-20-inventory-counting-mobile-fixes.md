# Inventory Counting (NetSuite) — mobile fixes

Date: 2026-10-20
Area: Mobile.MAUI `InventoryCountingItemView`, API `InventoryCounting/SaveScan`, NetSuite `inventorycount`

## Problems

1. The item screen had no search and no refresh.
2. A count could not be recorded as Zero.
3. Quantities saved earlier disappeared when the count was reopened.

## Root causes

| # | Cause | Location |
|---|-------|----------|
| 1 | No search/refresh UI; tapping the page heading reloaded the screen and wiped scans | `InventoryCountingItemView.razor:10`, `PageHeading.razor:1` |
| 2 | Manual entry rejected any all-zero result; save required `ScanCount > 0` | `ManualEntryDialog.razor.cs:32-36`, `InventoryCountingItemView.razor.cs:48` |
| 3 | The items query never returned a counted quantity and the screen reset every line to 0 | `NS_InventoryCounting_Get_Items.sql`, `InventoryCountingItemView.razor.cs:89-90,116-117` |
| 4 | Every line was sent, so untouched lines were written to NetSuite as 0 | `NetSuiteApiClientService.cs:2002` |
| 5 | `LEFT JOIN inventoryassignment` duplicated any line that has both Good and Bad detail rows | `NS_InventoryCounting_Get_Items.sql:29` |
| 6 | Bad quantities were never sent to NetSuite at all | `NetSuiteApiClientService.cs:2002` |

## What changed

### SQL — `Integration.NS/NSScripts/NS_InventoryCounting_Get_Items.sql`

- One row per count line (the join that duplicated lines is gone).
- New columns:
  - `CountedQuantity` = `tl.countquantity` (null until the line is counted).
  - `GoodCountedQuantity` / `BadCountedQuantity` = summed `inventoryassignment.quantity` by
    inventory status (1 = Good, 3 = Bad).
  - `NetsuiteInventoryDetailInternalId` = the Good detail row id.
  - `BadInventoryDetailId` = the Bad detail row id.

### API — DataTransferObjects

- `InventoryCountingLineDTO` / `InventoryCountingLineVM`: `BadInventoryDetailId`,
  `CountedQuantity`, `GoodCountedQuantity`, `BadCountedQuantity`.
- `InventoryCountingLineVM.IsCounted`: true when the operator touched the line this session.
- `PatchInventoryCountingDTO.IsCounted`: travels with the save request.
- `PatchInventoryCountingPayloadDTO`:
  - groups rows by `LineSequenceNumber`;
  - emits a Good row (status 1) and, when present, a Bad row (status 3);
  - sets `CountQuantity = Good + Bad`;
  - still emits one zero row for a line explicitly counted as 0, otherwise NetSuite would keep
    the previous system quantity;
  - status ids come from `InventoryStatusMap` (Good = 1, Bad = 3), the same source used by
    receiving and the inventory worksheet.

### API — `NetSuiteApiClientService.PatchInventoryCounting`

Only lines with `IsCounted` **or** a non-zero quantity are sent. The web page does not set
`IsCounted`, so its existing `ScannedQuantity > 0` behaviour is unchanged.

### Mobile — `InventoryCountingItemView`

- Search box (material code, name, UoM) and a refresh button, mirroring `PurchaseOrderItemView`.
  Refresh snapshots in-memory scans and restores them after the reload.
- Tapping the heading no longer reloads; `LoadInventoryCounting` was removed.
- Previously counted Good/Bad quantities are loaded from NetSuite instead of being reset to 0.
- Scans, Move, Remove and Manual Entry all set `IsCounted`; Save is enabled when any line is
  counted rather than when a scan counter is above zero.
- Manual Entry accepts an explicit zero (`AllowZero` parameter). The parameter defaults to
  `false`, so the other seven screens that share the dialog are unchanged.

## Decisions

- **Bad stock counts as physically present.** `CountQuantity = Good + Bad`, split across two
  detail rows. Confirmed by the business; matches the inventory worksheet flow.
- **Saving still exits to the count list.** Retention is visible when the count is reopened.
  This keeps the screen consistent with PO / TO / Returns / VRA.
- **Zero is entered through the existing manual dialog.** No new button was added.
- **Retention ends when the count leaves status `B`.** The items query only returns
  in-progress counts, so a closed count no longer appears on the phone.

## Assumptions to verify in sandbox

1. `transactionline.countquantity` is `NULL` until a line is counted and `0` when counted as
   zero. If the column does not exist, replace `tl.countquantity` with
   `NVL((SELECT SUM(ia.quantity) FROM inventoryassignment ia WHERE ia.transaction = t.id AND ia.transactionline = tl.id), 0)`
   and treat `CountedQuantity > 0` as the counted flag.
2. The `inventorycount` REST PATCH accepts two `countDetail.inventoryDetail.items[]` rows on one
   line and a zero quantity with `id: null`.
3. Inventory status ids 1 (Good) and 3 (Bad) are active in the target account. They are account
   configuration, not NetSuite constants — see `InventoryStatusMap.cs`.

## Verification

- `dotnet build Api.CoreWebAPI` — 0 errors.
- `dotnet build Mobile.MAUI` — 0 errors.
- `dotnet test Integration.NS.Tests` — 443 tests, all passing except
  `NetSuiteApiClientServiceTokenTests.HttpClient_has_explicit_timeout_of_100_seconds_by_default`,
  which passes when run alone and is order-dependent, unrelated to this change.
- New `PatchInventoryCountingPayloadTests` (5 tests) cover: untouched lines excluded, explicit
  zero sent, Good + Bad split, Bad-only line, and the web path that does not set `IsCounted`.

Manual checks still required against a sandbox count:

1. Count 3 of 50 lines, save, reopen — the 3 values are present and the other 47 are untouched
   in NetSuite (check `countquantity`, not 0).
2. Count one line as 0 via Manual, save, reopen — still 0.
3. Count 2 Good + 1 Bad on one line, save — NetSuite shows two detail rows (status 1 and 3).
4. Search filters both tabs; Clear restores; Refresh keeps unsaved scans.
5. Regression: the web inventory counting page still saves.

## Rollback

Revert the SQL file and the DTO/VM properties together. The payload builder tolerates a missing
`IsCounted` (it falls back to `ScannedQuantity > 0`), so an API-only rollback leaves the web path
working but reintroduces the untouched-line zeroing on mobile.
