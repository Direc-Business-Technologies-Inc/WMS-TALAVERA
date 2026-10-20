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
  - `GoodCountedQuantity` / `BadCountedQuantity` = summed `inventoryassignment.quantity` per
    inventory status, from two derived tables joined on their own output columns.
  - `NetsuiteInventoryDetailInternalId` = the Good detail row id (`MAX(ia.id)`).
  - `BadInventoryDetailId` = the Bad detail row id.
- Status predicates are quoted (`'1'` = Good, `'3'` = Bad), matching every other script in
  `NSScripts/`; `inventorystatus` is text in SuiteQL.
- `COALESCE(..., 0)` on the derived-table outputs, because
  `InventoryCountingLineDTO.GoodCountedQuantity` / `BadCountedQuantity` are non-nullable
  `decimal` and a NULL would fail deserialization.
- No `tl.countquantity` projection (see Follow-up below — the column name was never confirmed
  and the screen never read it).

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

### Mobile — `ApiClientService` (cross-cutting)

`Get<T>` / `Post<T>` / `Post` now return a failed `ApiResult` when the HTTP response is not
successful. Before this, the body was deserialized regardless of status: a 500 ProblemDetails
has no `errorMessage`, so `ApiResult.Success` (which is `ErrorMessage`-empty) reported success
with `Data = null`. Every server-side failure therefore reached `OnSuccess` as a null payload.
See `.opencode/docs/changes/2026-10-20-mobile-api-client-error-masking.md`.

## Follow-up (same day) — reopening a count crashed

**Symptom:** opening a count item on the phone showed `Value cannot be null. (Parameter
'source')` and an empty screen; the server-side error was not visible anywhere.

**Cause:** two defects stacked.

1. The items query added the counted quantities as four **scalar correlated subqueries**
   (`(SELECT SUM(ia.quantity) FROM inventoryassignment ia WHERE ia.transaction = t.id ...)`),
   the shape Rule 1 of `.opencode/docs/SuiteQL-Shape-Rules.md` records as rejected by NetSuite.
   It also compared `inventorystatus` to bare integers and projected `tl.countquantity`, a
   column referenced by no other script.
2. `ApiClientService` treated the resulting error response as success with a null payload, so
   `result.Data.Select(...)` in `InventoryCountingItemView` threw instead of surfacing the
   NetSuite message.

**Fix:**

- The four subqueries became two derived tables (`ig` for status 1, `ib` for status 3), joined
  on `TransactionId`/`TransactionLineId`; statuses are quoted; `tl.countquantity` is gone.
- `InventoryCountingItemView.OnSuccess` reads `result.Data ?? []`; the same dereference guard was
  applied to the four other screens that read `result.Data` directly. The two load actions now
  have `OnFailure` toasts, so a failed load names its cause instead of showing an empty screen.
- `ApiClientService` inspects the status code before deserializing (see above).

**Guard rails added:**

- `SuiteQLAssert.NoOuterAliasReferenceInDerivedTable` + `SuiteQLDerivedTableCorrelationTests`
  (4 tests) pin the Rule 1 shape, both directions. Scope is derived tables only: a
  single-level scalar subquery in the SELECT list is accepted by NetSuite in production
  (`NS_TO_Get_Itemfulfillment_Items.sql`), so flagging that class would force working queries
  to change.
- `NSScriptsWellFormedTests` gained three checks on this script: no `(SELECT`, quoted
  `inventorystatus`, no `.countquantity` projection.

## Decisions

- **Bad stock counts as physically present.** `CountQuantity = Good + Bad`, split across two
  detail rows. Confirmed by the business; matches the inventory worksheet flow.
- **Saving still exits to the count list.** Retention is visible when the count is reopened.
  This keeps the screen consistent with PO / TO / Returns / VRA.
- **Zero is entered through the existing manual dialog.** No new button was added.
- **Retention ends when the count leaves status `B`.** The items query only returns
  in-progress counts, so a closed count no longer appears on the phone.
- **A count is not capped by the planned quantity** (corrected same day). Manual Entry now passes
  `EnforcePlannedLimit = false` on the counting screen, so a line with `LineQuantity = 0` or a count
  above plan can be confirmed. See
  `2026-10-20-inventory-counting-manual-planned-qty.md`.

## Assumptions to verify in sandbox

1. The `inventorycount` REST PATCH accepts two `countDetail.inventoryDetail.items[]` rows on one
   line and a zero quantity with `id: null`.
2. Inventory status ids 1 (Good) and 3 (Bad) are active in the target account. They are account
   configuration, not NetSuite constants — see `InventoryStatusMap.cs`.
3. `SUM(inventoryassignment.quantity)` for status 3 is a positive number. If Bad stock is stored
   negative, the screen would show a negative Bad quantity and `CountQuantity = Good + Bad`
   would under-count; the payload builder would then need `Math.Abs`.

## Verification

- `dotnet build Api.CoreWebAPI` — 0 errors.
- `dotnet build Mobile.MAUI` — 0 errors.
- `dotnet test Integration.NS.Tests` — 477 tests. Two timing-sensitive tests
  (`NetSuiteApiClientServiceTokenTests.HttpClient_has_explicit_timeout_of_100_seconds_by_default`,
  `GridResultCacheTests.Key_index_does_not_outlive_the_cache_it_tracks`) fail intermittently in a
  full run and pass when run in isolation; neither touches this change. Includes the 5 payload
  tests and the 4 new Rule 1 lint tests.
- New `PatchInventoryCountingPayloadTests` (5 tests) cover: untouched lines excluded, explicit
  zero sent, Good + Bad split, Bad-only line, and the web path that does not set `IsCounted`.

Manual checks still required against a sandbox count:

1. Count 3 of 50 lines, save, reopen — the 3 values are present and the other 47 are untouched
   in NetSuite (check `countquantity`, not 0).
2. Count one line as 0 via Manual, save, reopen — still 0.
3. Count 2 Good + 1 Bad on one line, save — NetSuite shows two detail rows (status 1 and 3).
4. Search filters both tabs; Clear restores; Refresh keeps unsaved scans.
5. Regression: the web inventory counting page still saves.
6. A count whose query fails must now show the NetSuite message on the phone, not a null-argument
   crash — this is the observable proof that the client fix works.

## Rollback

Revert the SQL file and the DTO/VM properties together. The payload builder tolerates a missing
`IsCounted` (it falls back to `ScannedQuantity > 0`), so an API-only rollback leaves the web path
working but reintroduces the untouched-line zeroing on mobile.

Reverting `ApiClientService` alone is safe (it only changes how failures are reported), but it
restores the null-payload crash class for every mobile screen.
