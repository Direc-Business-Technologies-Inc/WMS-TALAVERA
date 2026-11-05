# Mobile API client swallowed every non-2xx response

Date: 2026-10-20
Area: `Mobile.MAUI/Services/ApiClientService.cs`
Trigger: the Inventory Counting reopen crash

## Symptom

Opening a count item threw `Value cannot be null. (Parameter 'source')` and left the screen
empty. The NetSuite error that caused it never appeared anywhere: not on the phone, not in the
API response the phone received.

## Cause

`ApiClientService` only special-cased 404 and 401. Every other status was parsed as if it were a
success body:

```csharp
return (!string.IsNullOrEmpty(res.Content)
    ? JsonSerializer.Deserialize<ApiResult<T>>(res.Content)
    : new()) ?? new();
```

`ApiResult.Success` is defined as `string.IsNullOrEmpty(ErrorMessage)`. An ASP.NET Core
exception response is ProblemDetails (`title`, `status`, `traceId`) — no `errorMessage` — so
deserialization left `ErrorMessage` at its `string.Empty` initializer:

| HTTP | Body | `Success` | `Data` |
|------|------|-----------|--------|
| 200 (query ok) | `{ "data": [...] }` | true | rows |
| 200 (query ok, no rows) | `{ "data": null }` | true | **null** |
| 500 (handler threw) | `{ "title": ..., "status": 500 }` | **true** | **null** |

Both failure columns fed `OnSuccess`, and any screen that started with
`result.Data.Select(...)` (or similar) crashed. The crash message was then the only diagnostic
the operator saw, and `ActionFactoryService`'s `showToast` path never ran because the request
had been classified as successful.

The 500 no-rows case is a separate, still-open hazard: `NetsuiteQuery` returns
`NetSuiteResponse<T>.items`, which is `null` when NetSuite omits the array, so a legitimate
empty result also produces `Data = null`. Screens must treat `null` as "no rows".

## Fix

`Get<T>`, `Post<T>` and `Post` now check `res.IsSuccessStatusCode` before deserializing and
return `ApiResult.Failed(...)`. The message prefers the API's own `ErrorMessage` when the body
is an `ApiResult`, and otherwise falls back to the status text
(`500 Internal Server Error`). Bodies that are not `ApiResult` no longer throw a
`JsonException` on the way out — the deserialize attempt is guarded.

`InventoryCountingItemView.OnSuccess` additionally reads `result.Data ?? []`, and the same guard
was applied to the four other screens that dereferenced `result.Data` directly:

- `Receiving/TransferOrder/TransferOrderItemView`
- `Receiving/Returns/ReturnsItemView`
- `Packing/Returns/ReturnsItemView`
- `Packing/VendorReturnAuthorization/VendorReturnAuthorizationItemView`
- `TripTicket/TripTicketDetailsView`

The Inventory Counting screen also gained `OnFailure` toasts for both load actions, because a
read that fails leaves an empty screen with no other feedback.

## Why this is the right layer

Every mobile save path already wraps NetSuite 400s as `ApiResult.ServerError(...)` inside the
command handler (see `.opencode/docs/mobile-netsuite-4xx-catalog.md`). Query paths have no such
wrapper: an exception in a MediatR query handler leaves the controller and is rendered by the
host as ProblemDetails. That asymmetry is what made reads break silently while writes showed a
message. Normalising on the client covers both.

## Blast radius

Server failures now reach `OnFailure` instead of `OnSuccess`. Screens that define no `OnFailure`
and pass `showToast: false` will look the same as before (an empty grid) but will no longer
crash; screens with `OnFailure` will show the message. Check these first after deploying:

- Inventory Counting list and item screens (item screen now toasts failures)
- Receiving (PO / TO / Returns / VRA) item screens
- Packing item screens
- Trip Ticket and Inventory Worksheet screens

## Still open

- Query handlers return `null` rather than an empty list when NetSuite sends no `items`, and the
  controllers wrap that as `Succeeded(null)`. The mobile guards absorb it; the web pages still
  see `null` from `GetStartedInventoryCountingLinesAsync` and similar methods. Normalising at the
  handler (`?? []`) is the durable fix and is not part of this change.

## Verification

- `dotnet build Mobile.MAUI` — 0 errors.
- `dotnet test Integration.NS.Tests` — 477 passing.
- Manual: force a failing query (e.g. temporarily point a script at a bad column) and confirm
  the phone shows the NetSuite/API message instead of a null-argument crash.

## Rollback

Revert `ApiClientService.cs` alone. The `result.Data ?? []` guard stays valid and keeps the
Inventory Counting screen from crashing, it just hides the cause again.
