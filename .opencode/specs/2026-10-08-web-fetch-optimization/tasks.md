# Tasks: Web Data-Fetch Optimization

**Date:** 2026-10-08
**Status:** Confirmed
**Mode:** FEATURE
**Spec:** design.md (same folder)
**Related:** requirements.md (same folder)

**Goal:** Reduce table-data fetch latency on page navigation via reference-data caching, cancellation propagation, debounced inputs, and short-TTL grid result caching.

**Technical context:** .NET 8, Blazor Server, MediatR, `IMemoryCache` (singleton), NetSuite via `NetSuiteApiClientService`. Build: `dotnet build` from solution root.

## Phase A — Reference-data caching (Req 1)

- **A1.** Fix single-flight race in `NsCacheExtensions.GetOrAddAsync` (`Integration.NS\Implementations\Others\Caching\NsCacheExtensions.cs:19-33`): add a static `ConcurrentDictionary<string, SemaphoreSlim>`; acquire per key before factory invocation. _Requirements: 1.4_
- **A2.** Create `CachedWarehouseMasterDataIntegration`, `CachedSchoolYearIntegration`, `CachedTransferTypeIntegration`, `CachedTransactionTypeIntegration`, `CachedDepartmentIntegration` in `Integration.NS\Implementations\Others\Caching\`, mirroring `CachedVendorIntegration.cs` (TTL 15 min, key = interface + method + subsidiary only, not full intent). _Requirements: 1.1, 1.2_
- **A3.** Register the new decorators in `NSImplementationDI.cs:52-66` (concrete + decorator factory pattern, same as Location/Subsidiary/Vendor). _Requirements: 1.1_
- **A4.** Add cache eviction: in mutation handlers for warehouses/school years/etc., call `IMemoryCache.Remove` on the known keys (or a small `IReferenceDataCacheInvalidator`). _Requirements: 1.3_
- **A5.** Checkpoint: `dotnet build`; manually verify second visit to a page with warehouse dropdown makes no NetSuite call. _Requirements: 1.1, 1.2_

## Phase B — Cancellation token propagation (Req 3)

- **B1.** Add `CancellationToken ct = default` to integration interfaces used by live grids (start with `IItemsIntegration`, `IWarehouseMasterDataIntegration`, `ILocationIntegration`, `ISubsidiaryIntegration`, `IVendorIntegration`, `IDepartmentIntegration`) and their NS implementations. _Requirements: 3.2_
- **B2.** Thread `ct` through MediatR query handlers (`Application.UseCases\Queries\...`) — `IRequestHandler<T,R>.Handle(req, ct)` already receives it; pass to integration calls. _Requirements: 3.2_
- **B3.** Thread `ct` through Blazor handlers (`Web.BlazorServer\Handlers\Implementations\...`, e.g. `ItemsHandler.cs:25-31`, `DeliveryHandler.cs:19`) into `sender.Send(query, ct)`. _Requirements: 3.2_
- **B4.** Add `ct` to `NetSuiteApiClientService.ExecuteSuiteQLQuery` and pass to the underlying `HttpClient` call. _Requirements: 3.1_
- **B5.** In Blazor components, create a `CancellationTokenSource` per load, cancel in `DisposeAsync`/navigation, and catch `OperationCanceledException` silently. _Requirements: 3.1, 3.3_
- **B6.** Checkpoint: `dotnet build`; navigate away mid-load → no error UI, no orphaned HTTP call. _Requirements: 3.1, 3.3_

## Phase C — Debounced grid inputs (Req 4)

- **C1.** Add `Debouncer` helper to `Shared.Libraries` (cancel previous `Task.Delay(400)`, then invoke). _Requirements: 4.1_
- **C2.** Apply in `ItemDataGrid.razor.cs:106-118`: debounce search text changes; Enter/blur fires immediately. _Requirements: 4.1, 4.2_
- **C3.** Apply in `QuickVirtualizedDropdown.razor.cs:54-90`: debounce filter text; cancel in-flight query via the Phase B token. _Requirements: 4.1, 4.3_
- **C4.** Checkpoint: `dotnet build`; type rapidly in dropdown → exactly one query after 400ms pause. _Requirements: 4.1, 4.3_

## Phase D — Grid result caching (Req 2)

- **D1.** Create `GridResultCache` helper (key = user id + entity + serialized `DataGridIntent` hash; TTL 60s; size-capped) in `Integration.NS\Implementations\Others\Caching\`. _Requirements: 2.1, 2.3_
- **D2.** Create cached decorators for live transactional integrations only (skip `NotImplementedException` stubs like `DeliveryIntegration`); wrap grid methods (`Get*DataGridAsync`). _Requirements: 2.1_
- **D3.** Register decorators in `NSImplementationDI.cs`. _Requirements: 2.1_
- **D4.** Eviction: mutation handlers for the same entity remove matching keys (prefix-based). _Requirements: 2.2_
- **D5.** Per-user scoping: include user id (from `IHttpContextAccessor`/auth state) in the key. _Requirements: 2.4_
- **D6.** Checkpoint: `dotnet build`; back-navigate within 60s → cached data, no NetSuite call; edit a row → next grid load refetches. _Requirements: 2.1, 2.2, 2.4_

## Optional

- **O1.** Extend cancellation/debounce to remaining grids (`DeliveryDataGrid`, `ITRTable`, etc.).
- **O2.** Metrics/logging on cache hit rate.

## Constraints

Do not modify `Integration.SAP`. Do not change grid pagination/sort/filter semantics. Do not cache exceptions. Follow `.claude/code_conventions.md` and `blazor_conventions.md`.
