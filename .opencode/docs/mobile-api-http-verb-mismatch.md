# Mobile ↔ API HTTP Verb Mismatch (Silent Empty Lists)

**Date:** 2026-10-14
**Status:** Fixed — builds clean; **not yet functionally verified on-device**
**Scope:** `Mobile.MAUI` only (8 call sites, 7 files). No API, DTO, VM, or markup changes.
**Related:** `.opencode/specs/2026-10-14-mobile-packaging-uom-item-cards/` (the UoM work whose
on-device test pass this bug blocked)

---

## Summary

Eight mobile call sites invoked `Client.Get<T>` against endpoints that the API exposes **only** as
`[HttpPost]`. ASP.NET Core answers those with **405 Method Not Allowed**, and because
`ApiClientService` special-cases only `404` and `401`, the response body falls through to
`JsonSerializer.Deserialize<ApiResult<T>>` — which either throws inside `AppAction` (swallowed) or
yields an empty result. Every affected `OnSuccess` handler then assigns `result.Data ?? []`, and the
screen renders a **silently empty list**.

There was no error, no toast, and no crash. From the user's side this looked exactly like "the
system has no data", which sent the investigation down the wrong path.

| Symptom reported | Actual cause |
|---|---|
| Location dropdown empty on Inventory Worksheet | `GET /Lookup/Susidiary/Locations` → 405 |
| Packing TO / Returns / VRA lists empty | `GET .../PendingFulfillment`, `GET .../PendingReturn` → 405 |
| Inventory Counting list empty | `GET /InventoryCounting/Started` → 405 |
| Receiving → Returns list empty | `GET /Receiving/Returns/PendingReceipt` → 405 |

This was **pre-existing** and unrelated to the UoM feature. It surfaced only because that feature's
test pass needed the Worksheet and Packing screens, both of which were unreachable.

---

## Root cause

Two halves.

### 1. Verb mismatch

`Api.CoreWebAPI` follows the convention that **queries taking parameters are POST**:

```csharp
// Api.CoreWebAPI/Controllers/Lookup/LookupController.cs:36
[HttpPost("Subsidiary/Locations")]
public async Task<ApiResult<IEnumerable<LocationVM>>> GetSubsidiaryLocations(RequestPerUserDTO req)

// :74
[HttpPost("BinLocations")]
public async Task<ApiResult<IEnumerable<BinVM>>> GetBinLocations(BinLocationRequestDTO req)
```

The mobile client called some of these with `Get`, and some with `Post` — inconsistently. That
inconsistency is the real defect; the verb on any single route is incidental.

`Mobile.MAUI/Services/ApiClientService.cs` sends exactly what the caller names:

```csharp
// :60  — Get
RestRequest request = new RestRequest(endpoint, Method.Get);
...
// :87  — Post
RestRequest request = new RestRequest(endpoint, Method.Post);
```

Both serialize `body` identically (`JsonSerializer.Serialize` + `AddJsonBody`), so switching the
verb required **no payload change** — the anonymous objects already matched their DTOs.

### 2. Silent failure on unexpected status codes

`ApiClientService.cs:71-82` handles two status codes and ignores everything else:

```csharp
RestResponse res = await Client.ExecuteAsync(request);
if (res.StatusCode == HttpStatusCode.NotFound)
    throw new Exception("404. The endpoint does not exist.");
else if (res.StatusCode == HttpStatusCode.Unauthorized)
{
    await NotifyUnauthorizedAsync(endpoint);
    throw new NotAuthorizedException();
}

return (!string.IsNullOrEmpty(res.Content)
    ? JsonSerializer.Deserialize<ApiResult<T>>(res.Content)
    : new())!;
```

A **405** reaches the `Deserialize` call with a problem-details body that is not an `ApiResult<T>`.
The result is either a swallowed `JsonException` inside `AppAction`, or a default-constructed
result. Either way the caller sees an empty list, never an error. `Post` has the same gap at
`:100-112`.

---

## Fix

Changed `Client.Get<T>` → `Client.Post<T>` at 8 call sites. One word each; no other change.

| File | Line | Endpoint | API verb |
|---|---|---|---|
| `Mobile.MAUI/.../InventoryCounting/InventoryWorksheet/CreateInventoryWorksheetView.razor.cs` | 103 | `/Lookup/Susidiary/Locations` | POST |
| `Mobile.MAUI/.../InventoryCounting/InventoryWorksheet/CreateInventoryWorksheetView.razor.cs` | 121 | `/Lookup/BinLocations` | POST |
| `Mobile.MAUI/.../TripTicket/TripTicketDetailsView.razor.cs` | 67 | `/Lookup/Susidiary/Locations` | POST |
| `Mobile.MAUI/.../Packing/TransferOrder/TransferOrderView.razor.cs` | 35 | `/Packing/TransferOrder/PendingFulfillment` | POST |
| `Mobile.MAUI/.../Packing/Returns/ReturnsView.razor.cs` | 34 | `/Packing/Returns/PendingFulfillment` | POST |
| `Mobile.MAUI/.../Packing/VendorReturnAuthorization/VendorReturnAuthorizationView.razor.cs` | 35 | `/Packing/VendorReturnAuthorization/PendingReturn` | POST |
| `Mobile.MAUI/.../Receiving/Returns/ReturnsView.razor.cs` | 37 | `/Receiving/Returns/PendingReceipt` | POST |
| `Mobile.MAUI/.../InventoryCounting/InventoryCountingView.razor.cs` | 31 | `/InventoryCounting/Started` | POST |

Diff: **8 insertions / 8 deletions** across 7 files.

### Why mobile-side and not API-side

The alternative was adding `[HttpGet]` twins to the controllers. Rejected: the API's
POST-for-parameterized-queries convention is already applied consistently and is the correct REST
choice here (non-trivial request bodies, no caching value). Adding GET aliases would double the
endpoint surface to accommodate a client-side inconsistency — and would leave the convention
looking optional. The mobile client was the inconsistent half, so the mobile client is what changed.

### Audit of remaining `Client.Get` calls

Re-grepped after the fix. The 9 survivors all map to genuinely `[HttpGet]` endpoints:

- `/Lookup/Locations`, `/Lookup/Drivers`, `/Lookup/Helpers`, `/Lookup/TruckPlateNumbers`,
  `/Lookup/Subsidiaries` — all `[HttpGet]` in `LookupController`
- `/TripTicket/ItemFulfillment/Packed` — `[HttpGet]`
- `/InventoryCounting/Worksheet/Items` — `[HttpGet]`

No mismatches remain.

---

## Verification

```
dotnet build   ->   0 Error(s), 1648 Warning(s)
```

All warnings are pre-existing nullability noise; none reference the changed lines.

**Code-verified only. Not yet functionally verified** (needs a running app):

- [ ] Inventory Worksheet: Location dropdown populates; Bin dropdown appears after selecting one.
- [ ] Packing → TO / Returns / VRA: lists load.
- [ ] Inventory Counting: started-counts list loads.
- [ ] Receiving → Returns: pending list loads.
- [ ] Trip Ticket details: location dropdown populates.
- [ ] Confirm the `405`s are gone (API log / dev tools) rather than merely masked.

---

## Deliberately out of scope

- **Hardening `ApiClientService` against unexpected status codes.** This is the fix that would have
  turned a silent empty list into a visible error, and it is a behaviour change affecting every
  call site in the app. Worth its own change; not smuggled into a one-word-verb fix.
- **The blank `Item:` / `UoM:` in the Weight dialog.** `PurchaseOrderItemView.razor.cs:803` calls
  `GetWeightAsync("", "")` for the toolbar Weight button, so both fields render empty. Pre-existing
  and arguably intentional (blanket weight for a whole selection), but it reads as broken to a
  tester. Cosmetic, unrelated, separate ticket.
- **Committing alongside the UoM feature.** The UoM work is verified and should stay a clean diff.

---

## Operational notes

- **A clean `200` with an empty array is a *different* diagnosis from what was seen before.** If a
  list is still empty after this fix, the verb bug is gone and the remaining cause is genuinely "no
  data for this user + subsidiary". Don't re-file it as this bug.
- **This class of defect is invisible in CI.** There is no mobile test project, and no contract test
  asserts that mobile call verbs match controller attributes. The mismatch was found by eye, on
  device. If this recurs, the durable fix is a test that reflects over the controllers and asserts
  each mobile endpoint is reachable by its declared verb — not another manual sweep.
- **Convention to follow:** if an API route is parameterized, it is POST. Match the neighbouring
  working call site rather than guessing — the Receiving PO and Receiving TO list loaders
  (`PurchaseOrderView.razor.cs:35`, `TransferOrderView.razor.cs:38`) were already correct and are
  the reference for what these eight should have looked like.
- **Follow-up:** after the on-device pass, tick the verification checklist above and update the
  Status line at the top of this document.
