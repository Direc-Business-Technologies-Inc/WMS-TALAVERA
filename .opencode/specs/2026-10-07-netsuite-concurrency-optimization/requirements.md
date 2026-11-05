# Requirements — NetSuite Concurrency Optimization (WMS ↔ NetSuite)

```text
Date: 2026-10-07
Status: Confirmed
Mode: FEATURE
Scope: NetSuite outbound-call governance, retry, write queueing, and reference-data caching across Web.BlazorServer, Api.CoreWebAPI, and a new background worker
Related: None found (reference doc: .opencode/docs/WMS_NetSuite_Request_Flow.md)
```

## Introduction

The WMS shares one NetSuite account (concurrency limit **5**, observed peak **8**) across two server processes. NetSuite governs REST + RESTlet calls in one combined account-level pool; the WMS currently bursts past the limit because its limiters are uncoordinated and its OAuth1 path is ungated. This feature establishes a single shared concurrency gate, safe retries, a durable write queue with idempotent posting, and caching for stable reference data — matching the target flow in `WMS_NetSuite_Request_Flow.md`.

## Glossary

- **NetSuite concurrency governance** — account-level limit on simultaneous web-service/RESTlet requests (all types combined). This account: 5.
- **Permit / slot** — one unit of the shared gate. One permit = one in-flight NetSuite request.
- **Gate** — the shared Redis-backed concurrency limiter all NetSuite-bound calls must pass through.
- **OAuth1 path** — `MakeRequestOAuth1` (RESTlet calls, HMAC-SHA1-style signing).
- **OAuth2 path** — `MakeRequest` (REST/record/SuiteQL calls, bearer token).
- **Stable reference data** — rarely changing NetSuite master data (Locations, Subsidiaries, Vendors).
- **Transactional data** — frequently changing operational data (inventory levels, fulfillments, order status). Never cached.
- **Operation ledger** — SQL table recording every queued NetSuite write operation and its lifecycle state.
- **Idempotency key** — the WMS transaction ID stamped into each NetSuite write payload; used for check-before-create on retries.
- **Fail-closed** — when the gate is unavailable, no NetSuite call proceeds ungated; requests wait a bounded time, then surface a user-visible busy state.
- **Worker** — a .NET `BackgroundService` that drains the ledger and posts writes to NetSuite.

## User Stories

1. As a warehouse user, I want saves to return instantly so my screen never freezes while NetSuite processes in the background.
2. As a warehouse user, I want busy periods to mean a short wait in a queue, not a "too many requests" error or a lost save.
3. As a warehouse user, I want each transaction posted to NetSuite exactly once, even if a request times out and is retried.
4. As a warehouse user, I want reference data (locations, subsidiaries, vendors) to load instantly because it rarely changes.
5. As an operations lead, I want to see the status of every queued NetSuite operation (pending/processing/done/failed) so I can resolve stuck work.
6. As an integration owner, I want the WMS never to exceed its allocated NetSuite concurrency so other integrations and scripts keep their slots.

## Requirements

### 1. Global concurrency governance

- **1.1** Every NetSuite-bound call from every WMS process — OAuth2 path (`MakeRequest`), OAuth1 path (`MakeRequestOAuth1`), and login-time identity/location calls — SHALL pass through one shared gate.
- **1.2** The gate SHALL permit at most **3 concurrent** NetSuite requests account-wide (limit 5; 2 left free for other integrations/scripts).
- **1.3** The gate SHALL be shared by all processes that call NetSuite: `Web.BlazorServer`, `Api.CoreWebAPI`, and the worker.
- **1.4** The gate SHALL fail closed: if the gate backend is unreachable, no NetSuite call proceeds ungated. Interactive calls wait a bounded window (~30–60 s) then surface a user-visible "system busy, try again" result.
- **1.5** A permit SHALL be released only when NetSuite responds — never when the caller times out — so NetSuite-side in-flight work is never over-admitted.
- **1.6** Each permit SHALL carry an expiry (TTL) longer than the slowest expected NetSuite call, so a crashed process cannot hold a permit forever.

### 2. Removal of uncoordinated limiters

- **2.1** The form-level `SemaphoreSlim(2,2)` instances in `TripTicketCVU.razor.cs:55`, `STRForm.razor.cs:61`, `ITRForm.razor.cs:50`, `SupplierReturnForm.razor.cs:52` SHALL be removed (they are per-component-instance and provide no global protection).
- **2.2** The separate `_netsuiteSemaphore` in `AuthenticationController.cs:29` SHALL be removed; login-time NetSuite calls SHALL use the shared gate.
- **2.3** The in-process `_requestSemaphore` (`NetSuiteApiClientService.cs:210`) SHALL be removed or reduced to a local fast-path guard that cannot exceed the shared gate's limit.

### 3. Token handling fixes

- **3.1** The premature `_tokenExpiryTime = AddMinutes(30)` write in `GetAccessToken` (`NetSuiteApiClientService.cs:128`) SHALL be removed; expiry SHALL be set only by the token manager after a successful refresh.
- **3.2** The RSA private key SHALL be loaded once and cached; `GetJwtToken` SHALL NOT read the key file on every refresh (`:164`).
- **3.3** The shared `HttpClient` SHALL have an explicit timeout well below NetSuite's 15-minute server-side cutoff.

### 4. Typed NetSuite exceptions

- **4.1** NetSuite failures SHALL be thrown as typed exceptions distinguishing at minimum: transient (HTTP 429/408/5xx, network errors) vs. validation/business (4xx with parsed error detail).
- **4.2** Existing error message content surfaced to users SHALL be preserved (same parsed detail strings).

### 5. Retry policy (Polly v8)

- **5.1** Only transient failures (per 4.1) SHALL be retried; validation failures SHALL fail immediately.
- **5.2** Interactive (user-facing) calls SHALL use a short retry budget (1–2 attempts) and then hand off to the queue or surface an error.
- **5.3** Worker-side retries SHALL use the long schedule with jitter — 5 s, 15 s, 45 s, 2 m, 5 m — capped at a 15-minute total budget.
- **5.4** Retries SHALL use jittered delays to avoid synchronized retry storms.

### 6. Write path: operation ledger + background worker

- **6.1** NetSuite write operations SHALL be recorded in a SQL operation ledger (states: pending → processing → done / failed) before any NetSuite call is attempted.
- **6.2** Every ledger operation SHALL carry the WMS transaction ID as its idempotency key, stamped into the NetSuite payload; the worker SHALL check-before-create so a retry after a timeout never posts a duplicate.
- **6.3** A .NET `BackgroundService` worker SHALL drain the ledger and post writes through the shared gate, with worker concurrency within the gate's permit budget.
- **6.4** A status endpoint SHALL expose per-operation state so the UI can show done/needs-review and operations can resolve stuck entries.
- **6.5** If the gate is unavailable, ledger writes SHALL still be accepted (enqueue does not require the gate); the worker retries until the gate returns — fail-closed degrades to *delayed*, never *lost* or *duplicated*.

### 7. Reference-data caching

- **7.1** Stable reference data — **Locations, Subsidiaries, and Vendors** (amended 2026-10-07) — SHALL be cached with a 6-hour TTL.
- **7.2** Transactional data SHALL NOT be cached — reads remain fresh from NetSuite.
- **7.3** A manual cache-flush mechanism SHALL exist for operators.

### 8. Observability & load validation

- **8.1** The system SHALL record max observed concurrent NetSuite requests (the metric NetSuite governs by).
- **8.2** A load test against a fake NetSuite (scripted 429s/delays) SHALL verify: max concurrency ≤ 3; zero duplicate postings after forced timeouts; acceptable p95 save-to-ack latency at 30–50 concurrent users.

## Acceptance Criteria

- **AC1** WHEN 30+ users save simultaneously THEN the WMS SHALL issue at most 3 concurrent NetSuite requests and every save SHALL be acknowledged within ~1 s as received.
- **AC2** WHEN a NetSuite write times out after the record was created THEN the retry SHALL NOT create a duplicate (idempotency key check).
- **AC3** WHEN NetSuite returns 429 THEN the call SHALL be retried with jitter per §5 and SHALL NOT be surfaced as an immediate user error while budget remains.
- **AC4** WHEN NetSuite returns a validation error (e.g., insufficient stock) THEN no retry SHALL occur and the user SHALL see the parsed error detail.
- **AC5** WHEN the gate backend is down THEN no ungated NetSuite call SHALL be issued; queued writes SHALL remain in the ledger and process when it recovers.
- **AC6** WHEN a process crashes mid-request THEN its permit SHALL expire via TTL and the system SHALL recover without manual intervention.
- **AC7** WHEN reference data is requested twice within the TTL THEN the second read SHALL be served from cache without a NetSuite call.
- **AC8** WHEN transactional data is requested THEN it SHALL always come fresh from NetSuite.

## In-Scope

- Shared Redis concurrency gate (3 permits, fail-closed, TTL leases) in `Integration.NS` + both hosts + worker.
- **Redis provisioning** (no instance exists today — standing one up is an explicit task; hosting decision in design D10).
- Removal of decorative/uncoordinated semaphores.
- Token handling fixes (§3) and typed exceptions (§4).
- Polly v8 retry pipelines (§5).
- SQL operation ledger, migration, idempotent tracking, status endpoint, `BackgroundService` worker in `Api.CoreWebAPI` (§6).
- Reference-data cache (Locations, Subsidiaries, Vendors) with flush (§7).
- Load-test harness with fake NetSuite (§8) — new test projects: xUnit + WireMock.Net for correctness, NBomber for load (decision 2026-10-07).

## Out-of-Scope

- Azure Service Bus (deferred by decision 2026-10-07).
- Priority slot split (2 normal + 3 priority) — deferred; single 3-permit gate.
- Routing Blazor through the API / consolidating to one NetSuite-calling process (rejected by decision 2026-10-07).
- SuiteCloud Plus license purchase.
- Any mobile (`Mobile.MAUI`) changes — verified clean; it talks to the API only.
- Changes to NetSuite-side scripts/restlets beyond the verified check-before-create (design D8).
- Items caching (deferred; same decorator pattern can add it later).

## Preservation Requirements

- NetSuite payload formats and restlet contracts SHALL NOT change (except adding the idempotency key field where NetSuite scripts tolerate it — verify before relying).
- Existing UI flows and page behavior SHALL NOT change except where saves become asynchronous (instant "received" acknowledgment).
- Existing parsed error-detail strings shown to users SHALL be preserved.
- No secrets or connection strings in committed configuration.
- Transactional reads SHALL remain fresh (no caching) — current behavior preserved.

## Resolved Decisions (recorded from discussion, 2026-10-07)

| Question | Resolution |
|---|---|
| Redis availability | No existing instance; provisioning is an in-scope task |
| Cache scope | Locations, Subsidiaries, Vendors (Items deferred) |
| Worker host | `Api.CoreWebAPI` — keeps background work out of the Blazor UI process; API is the integration edge; status endpoint lives there |
| Test tooling | xUnit + WireMock.Net (correctness) + NBomber (load); new test projects required (none exist) |
| ASB | Deferred — SQL ledger-as-queue instead |
| Fail mode | Fail-closed with queueing (never duplicate postings) |
| Priority slots | Deferred — single 3-permit gate |
| One-process consolidation | Rejected — Redis gate covers present (2 hosts) and future (worker) processes |
