# Mobile Review: Auth Logout on Save + Item Fulfillment Sublist Error

Date: 2026-10-14
Status: Draft
Mode: RCA / Review
Scope: Mobile.MAUI authentication/session handling and the NetSuite Item Fulfillment posting path (STR, Returns, RTS)
Related: [Packing labels trip-ticket exemption RCA](2026-10-05-packing-label-trip-ticket-exemption-rca.md); [Exempt and non-exempt labels RCA](2026-10-05-packing-label-exempt-and-non-exempt-web-mobile-rca.md)

## Summary

Two issues were raised during this review session:

1. **Mobile logs out and the transaction is lost upon saving** (reported during a PO IR transaction).
2. **NetSuite 400 "invalid sublist or line item operation" when posting GOOD TO Item Fulfillment**, which the user recalls the web also used to throw.

Both were investigated via repository inspection only. No live NetSuite postings, device tests, or builds were performed.

---

## Issue 1: Mobile logs out on save; transaction not saved (PO IR)

### Symptom

While transacting a PO IR on Mobile, saving caused the app to log out and the transaction was not saved. A later refresh restored the session.

### Root Cause

A token-expiry race in the auth layer, combined with the absence of any token refresh mechanism:

1. **`JWTAuthStateProvider.cs:60-65`** — `IsTokenExpired` treats a token with <= 1 minute remaining as expired. `GetAuthenticationStateAsync` (lines 21-25) then calls `NotifyUserLogout()`, which **deletes the access token from SecureStorage**. Any navigation or re-render that re-evaluates auth state at that moment silently logs the user out with no dialog or warning.
2. **`AuthenticationService.cs:59`** — the JWT is issued with `expires: DateTime.Now.AddMinutes(480)` using **local server time**, but JWT `ValidTo` is interpreted as **UTC**. Server timezone skew makes tokens expire early (or late) relative to validation. Should be `DateTime.UtcNow`.
3. **No refresh mechanism.** The API exposes only `Login` (`AuthController.cs:15`). After 8 hours (minus timezone skew, minus the 1-minute buffer), the token is dead and re-login is the only recovery.

**Failure sequence:** user scans through a long session -> token crosses the 1-minute threshold (or the 8-hour lifetime) -> the Remarks dialog / confirm dialog / navigation triggers an auth-state evaluation -> `NotifyUserLogout()` wipes the token -> the `SaveScan` POST goes out unauthenticated or the page re-renders as `NotAuthorized` (`MainLayout.razor:41`) -> scanned state is lost.

Additionally, `ApiClientService.cs:70-72` throws a generic `"Not authorized."` on HTTP 401 but does not notify the auth provider or attempt recovery; the exception is swallowed by `ActionFactoryService.cs:57-61` into a toast and the in-memory scan session is gone.

### Blast Radius

Every mobile flow shares `ApiClientService` + `JWTAuthStateProvider`, so all are exposed to the same failure:

- Receiving: PurchaseOrder `SaveScan`, Returns `SaveScan`, TransferOrder `SaveScan`
- Packing: Returns, TransferOrder, VendorReturnAuthorization `SaveScan`
- InventoryCounting: `SaveScan` and `Worksheet/SaveScan`
- TripTicket: `SaveScan`
- Any long-running page where the user sits past token expiry before acting

### Contributing Findings

- **`NotAuthorizeErrorHandler.cs`** is an empty stub. Its `catch (SecurityTokenExpiredException)` can never fire because `ApiClientService` throws a generic `Exception`, not a `SecurityTokenExpiredException`. It was clearly meant to be the 401 handler and was never wired up.
- **`UnsaveChangesHandlerService.cs`** exists (dirty-state tracking) but is not used by any transaction view, so nothing warns the user before a silent logout destroys their scan session.
- **`JWTAuthStateProvider.cs:74`** — `GetAccessToken()` uses `.Result` (sync-over-async); latent deadlock risk.
- Good hygiene elsewhere: `DisposeAsync` in the page views properly unsubscribes `BroadcastService` events; scan validation (over-scan / exceed remaining qty) is thorough.

### Corrective Direction

1. Fix the clock bug: `expires: DateTime.UtcNow.AddMinutes(480)` (`AuthenticationService.cs:59`).
2. Stop treating < 1 min remaining as grounds for logout. Add a refresh endpoint (sliding session re-issuing a token for an authenticated caller) and let the 1-minute buffer trigger a refresh attempt instead of `NotifyUserLogout()`.
3. Centralize 401 handling in `ApiClientService`: on 401, notify the auth provider and navigate to login with a message, instead of throwing a generic exception that dies as a toast.
4. Wire up `UnsaveChangesHandlerService` in transaction views so expiry/logout prompts "You have unsaved scans" instead of silently discarding them.
5. Longer term: refresh-token flow so a shift worker is not forced to re-login mid-pick every 8 hours.

---

## Issue 2: NetSuite 400 "invalid sublist or line item operation" on GOOD TO Item Fulfillment

### Symptom

After the session was recovered by refreshing, saving a packing transaction failed with:

```
Error while posting GOOD TO Item Fulfillment. Netsuite 400 Bad Request:
[USER_ERROR] You have attempted an invalid sublist or line item operation.
You are either trying to access a field on a non-existent line or you are
trying to add or remove lines from a static sublist
```

The user recalls the web previously threw the same error.

### Root Cause (regression, high confidence)

The Item Fulfillment payloads send the wrong `inventoryDetail` shape AND lost the `?replace=` query parameter that was originally added to fix this exact error. The error message uniquely originates from `NetSuiteApiClientService.cs:1109` (`SaveTOItemFulfillment`, GOOD branch).

**Working shape — Item Receipts** (`ReturnsIRPayloadDTO.cs:82-85`; same in `ItemReceiptTransformPayload.cs:144-151`):

```json
"inventoryDetail": { "inventoryAssignment": { "items": [ ... ] } }
```

**Failing shape — all three IF payloads** (`TransferOrderIFPayloadDTO.cs:114-120`; identical in `ReturnsIFPayloadDTO.cs:116` and `VendorReturnAuthorizationIFPayloadDTO.cs:116`):

```json
"inventoryDetail": { "inventoryAssignmentList": { "inventoryAssignment": { "items": [ ... ] } } }
```

The `inventoryAssignmentList` wrapper is the **SOAP/SuiteTalk schema name**, not the REST record v1 sublist name (`item.inventoryDetail.inventoryAssignment` — confirmed by the replace param your own Returns IR URL uses at `NetSuiteApiClientService.cs:1021`, and by the working receipt payload shape).

### Git History

1. **`5450579` (2026-07-14) — "IF Payload Adjustments and TO Queries"**: **added** `?replace=item.inventoryDetail.inventoryAssignment` to `ItemFulfillmentUrl`. This was the fix for the error remembered from web.
2. **`1a100a9` (2026-08-14) — "Posting Adjustments and Update Location Retain of Lines"**: **removed** it (commented out at `NetSuiteApiClientService.cs:45-47`), splitting the URL into used-bin / not-used-bin variants — both without `replace`.
3. **`4b062dc` (2026-08-26) — "Trip Ticket Adjustment"**: changed all three IF payload DTOs to **always** use the `inventoryAssignmentList` wrapper, commenting out the REST-shape branch previously used for used-bin locations.

Both halves of the July fix were backed out in August. It appears intermittent because it only bites when the transform pre-populates the inventory assignment sublist (used-bin TOs / lines with pre-assigned bins); plain non-bin lines can slip through.

### Why PO IR Still Works

Item *receipts* (PO IR, TO receiving via restlet, Returns IR) use the correct `inventoryDetail.inventoryAssignment` shape; the Returns IR URL even re-adds the `?replace=` param (`NetSuiteApiClientService.cs:1021`). Only the *fulfillment* path regressed.

### Blast Radius

All three fulfillment flows share the broken shape and replace-less URL:

- **STR Packing** -> `SaveTOItemFulfillment` (`NetSuiteApiClientService.cs:1067`) — currently failing
- **Returns Packing** -> `SaveReturnsItemFulfillment` (`:1115`)
- **RTS / Vendor Return Authorization** -> `SaveVRAItemFulfillment` (`:1141`)

Not affected: PO IR, TO receiving (restlet), Returns IR.

### Data-Integrity Concern (follow-on)

The BAD IF posts **before** the GOOD IF (`NetSuiteApiClientService.cs:1079-1111`) with no compensation. If BAD succeeds and GOOD fails, the transfer order is left partially fulfilled; a naive retry then fails differently (duplicate fulfillment on the bad side). Verify the affected TO's state in NetSuite before re-testing.

### Corrective Direction

1. Restore the REST shape in the three IF payload DTOs, mirroring `ReturnsIRPayloadDTO.cs:82-85`:

   ```csharp
   var inventoryDetail = new InventoryDetail
   {
       InventoryAssignment = assignment   // not InventoryAssignmentList
   };
   ```

2. Re-add the replace param to the IF transform URLs (`NetSuiteApiClientService.cs:46-47`):

   ```csharp
   private static readonly string ItemFulfillmentUrl =
       $"https://{AccountId}.suitetalk.api.netsuite.com/services/rest/record/v1/{{0}}/{{1}}/!transform/itemFulfillment?replace=item.inventoryDetail.inventoryAssignment";
   ```

3. Log the serialized payload in the catch blocks of the fulfillment methods — currently a NetSuite 400 dies with no payload trace, which is why this diagnosis required archaeology across three commits.
4. Reconcile the earlier failed attempt in NetSuite (partial BAD fulfillment) before re-testing the affected order.

---

## Significant Findings (whole session)

| # | Finding | Location | Severity |
|---|---------|----------|----------|
| 1 | Token considered expired at <= 1 min remaining; logout silently deletes the token | `JWTAuthStateProvider.cs:60-65` | High |
| 2 | JWT issued with `DateTime.Now` (local) instead of UTC; timezone skew changes effective lifetime | `AuthenticationService.cs:59` | High |
| 3 | No refresh endpoint exists; 8-hour token is the only session lifespan | `AuthController.cs:15` | High |
| 4 | 401 thrown as generic `Exception`; no logout, no refresh, dies as a toast | `ApiClientService.cs:70-72`, `ActionFactoryService.cs:57-61` | Medium |
| 5 | `NotAuthorizeErrorHandler` is an empty stub catching an exception type that is never thrown | `NotAuthorizeErrorHandler.cs` | Medium |
| 6 | `UnsaveChangesHandlerService` exists but is unused; unsaved scans are discarded without warning | `UnsaveChangesHandlerService.cs` | Medium |
| 7 | `GetAccessToken()` uses `.Result` (sync-over-async deadlock risk) | `JWTAuthStateProvider.cs:74` | Low |
| 8 | All three IF payloads send SOAP-shaped `inventoryAssignmentList` to the REST endpoint | `TransferOrderIFPayloadDTO.cs:114-120` (+ Returns, VRA variants) | High |
| 9 | `?replace=item.inventoryDetail.inventoryAssignment` fix from 2026-07-14 was removed on 2026-08-14 | `NetSuiteApiClientService.cs:45-47` | High |
| 10 | BAD-then-GOOD posting without compensation leaves partially fulfilled TOs after a failure | `NetSuiteApiClientService.cs:1079-1111` | Medium |
| 11 | No payload logging on NetSuite posting failures | `NetSuiteApiClientService.cs` catch blocks | Medium |

## Validation Criteria

- **Auth:** reproduce a token within 1 minute of expiry during a scan session; verify the save either succeeds via refresh or prompts with unsaved-changes, and the session recovers without data loss.
- **IF posting:** save one used-bin STR (good + bad lines, partial quantities) end-to-end on Mobile and Web; confirm both IFs are created, status `C`/`B` per exemption, and quantities/bins/statuses match the scan session.
- Repeat for Returns packing and RTS; confirm Item Receipts (PO IR, TO receiving, Returns IR) are unaffected.
- Confirm a failed GOOD post mid-sequence leaves no ambiguous TO state (or is detected and surfaced).

## Deferred Questions and Validation Limits

- Live NetSuite behavior was not exercised; fixes should be verified against the target account before release.
- Server timezone of the API host was not verified; the `DateTime.Now` impact size depends on it.
- The external NetSuite print script behavior for `custbody_dbti_print_label` remains outside this repository (per prior RCAs).
- Refresh-token/session design (sliding window vs refresh token) needs a product decision before implementation.
