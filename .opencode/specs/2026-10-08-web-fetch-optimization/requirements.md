# Requirements: Web Data-Fetch Optimization

**Date:** 2026-10-08
**Status:** Confirmed
**Mode:** FEATURE
**Scope:** Web.BlazorServer, Integration.NS, Application.UseCases
**Related:** None found

## Introduction

Reduce perceived latency when users navigate between pages by (a) caching rarely-changing reference data, (b) propagating cancellation tokens through the fetch pipeline, (c) debouncing grid search/filter inputs, and (d) caching grid page results briefly.

## Glossary

- **Reference data:** warehouses, school years, transfer types, transaction types, delivery means, departments, locations, subsidiaries, vendors.
- **Grid intent:** `DataGridIntent` (filters, sort, skip/take).
- **Circuit:** a Blazor Server user session.

## Requirements

### 1. Reference-data caching
- 1.1 WHEN a page requests reference data (warehouses, school years, transfer types, transaction types, delivery means, departments) THEN the system SHALL serve it from `IMemoryCache` when a fresh entry exists, and SHALL call NetSuite only on miss or expiry.
- 1.2 WHEN a reference-data entry is cached THEN it SHALL expire after a configurable TTL (default 15 minutes).
- 1.3 WHEN a mutation command modifies a reference-data entity THEN the system SHALL evict the corresponding cache entries.
- 1.4 WHEN the same reference data is requested concurrently THEN only one NetSuite call SHALL be made (single-flight).

### 2. Grid result caching
- 2.1 WHEN a grid requests a page of results THEN the system SHALL serve it from cache when an entry exists for the same page + intent hash and is within TTL (default 60 seconds).
- 2.2 WHEN a mutation affects the underlying entity THEN cached grid entries for that entity SHALL be evicted.
- 2.3 WHEN cache capacity is exceeded THEN least-recently-used entries SHALL be evicted first.
- 2.4 Grid caching SHALL be scoped per user circuit; no data SHALL leak across users.

### 3. Cancellation token propagation
- 3.1 WHEN a Blazor component is disposed or the user navigates away mid-fetch THEN the in-flight NetSuite HTTP request SHALL be cancelled.
- 3.2 Cancellation tokens SHALL flow: component → handler → MediatR query → integration → `NetSuiteApiClientService`.
- 3.3 WHEN a fetch is cancelled THEN no exception SHALL surface to the user; the UI SHALL simply stop updating.

### 4. Debounced grid inputs
- 4.1 WHEN a user types in a grid search box or virtualized dropdown filter THEN the system SHALL wait 400ms of inactivity before issuing a query.
- 4.2 WHEN the user presses Enter or the input loses focus THEN the query SHALL fire immediately.
- 4.3 WHEN a new debounced query fires THEN any pending or in-flight query for the same grid SHALL be cancelled.

### 5. Preservation
- 5.1 Existing pagination, sorting, and filtering behavior SHALL remain unchanged.
- 5.2 Existing error handling and busy-state behavior SHALL remain unchanged, except that cancelled fetches SHALL NOT show error UI.
- 5.3 The SAP integration project SHALL NOT be modified.

## Out of Scope

Parameterizing/sanitizing SuiteQL, removing the COUNT query, prefetching, API (mobile) endpoint changes, fixing `Task.Yield()` crutches.
