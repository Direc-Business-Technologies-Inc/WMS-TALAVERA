# Requirements — NetSuite Interim Code-Only Optimization (WMS ↔ NetSuite)

```text
Date: 2026-10-07
Status: Confirmed
Mode: FEATURE
Scope: Code-only interim NetSuite optimization on the write/read request path — unified reduced in-process limiter, Polly retry with write-safe semantics, per-process reference-data caching. No database changes, no new infrastructure, no package downloads beyond the local NuGet cache.
Related: .opencode/specs/2026-10-07-netsuite-concurrency-optimization (parent; its Tasks 4/5/6/9/10 verified complete in code); reference doc .opencode/docs/WMS_NetSuite_Request_Flow.md
```

## Introduction

The WMS exceeds its NetSuite concurrency budget (limit 5, observed peak 8) because `MakeRequest`'s limiter is per-process at 4 across two hosts. The client forbids any database change, and the environment forbids downloads/Docker/new infrastructure — so the parent spec's shared gate (Redis/SQL) and durable write ledger are unavailable.

This feature hardens the most-used path — the OAuth2 `MakeRequest` write/read channel — using only code and locally cached packages: one unified in-process limiter sized so two hosts cannot exceed the account limit, retries that are safe for writes (429-only), and per-process caching of stable reference data to free slots for writes. Every choice is made behind seams the parent spec defined, so the Redis/SQL-backed versions can replace them later without caller changes.

## Glossary

- **Hot path** — `MakeRequest` (`NetSuiteApiClientService.cs:293`): all SuiteQL reads (POST to `/query/v1/suiteql`) and all record/restlet writes (POST/PUT/PATCH).
- **Interim limiter** — the single static `SemaphoreSlim` shared by both request paths, replacing `new(4,4)` at `:253`.
- **Write** — any request that mutates NetSuite: record/restlet POST/PUT/PATCH (URL under `/record/v1/` or a restlet endpoint).
- **Read** — SuiteQL queries (URL under `/query/v1/`) and any GET; reads never mutate, so they are always safe to retry.
- **429-safe retry** — retrying only when NetSuite rejected for concurrency (429), which guarantees the request never executed; the only retry that cannot create a duplicate write.
- **Interim cache** — per-process `IMemoryCache` decorators; no cross-process invalidation.

## User Stories

1. As a warehouse user, I want saves/posts to stop failing with NetSuite concurrency errors during busy periods.
2. As a warehouse user, I want a 429 to be retried quietly once instead of flashing an error.
3. As a warehouse user, I want my transaction posted **exactly once** — never duplicated by a retry.
4. As a warehouse user, I want locations/subsidiaries/vendors to load instantly.
5. As an integration owner, I want worst-case account-wide concurrency ≤ 5 enforced by configuration, without touching the database.

## Requirements

### 1. Unified interim limiter

- **1.1** `MakeRequestOAuth1` (`:455-570`) SHALL pass through the same limiter as `MakeRequest` before any send (defense-in-depth; currently no active callers).
- **1.2** The limiter SHALL be **one** static `SemaphoreSlim` shared by both paths — not one per path (two semaphores of 2 would sum to 4 per process, 8 account-wide).
- **1.3** The limiter size SHALL be **2 by default**, configurable via environment variable (following the existing `NETSUITE_HTTP_TIMEOUT_SECONDS` pattern at `:94-98`), yielding worst case 2 × 2 hosts = 4 ≤ account limit 5.
- **1.4** No NetSuite-bound call SHALL execute outside the limiter (token-refresh calls excepted — documented: they target the auth endpoint and are rare).
- **1.5** The limiter SHALL release in `finally` on all exits including caller timeout (an in-process semaphore has no TTL; a leaked permit would deadlock the process). This consciously deviates from parent property 3, which is deferred to the real gate.

### 2. Write-safe retry (Polly v8, pinned 8.4.2 from local cache)

- **2.1** Only `NetSuiteTransientException` SHALL be retried; `NetSuiteValidationException` SHALL never be retried; user-visible message strings SHALL remain byte-identical.
- **2.2** Interactive profile (the only profile in this phase): at most 1 retry with ~5 s jittered delay.
- **2.3** **Writes SHALL retry on 429 only.** Timeout/408/5xx/network failures on a write SHALL surface immediately as the existing parsed error — never retried (duplicate risk).
- **2.4** **Reads SHALL retry on any transient failure** (429/408/5xx/network).
- **2.5** Read vs. write SHALL be classified by URL and method (URL contains `/query/v1/` or method is GET → read; otherwise write), overridable by an explicit parameter on `MakeRequest`/`MakeRequestOAuth1` for call sites that don't match the rule.
- **2.6** The retry API SHALL accept an optional profile/kind parameter with safe defaults so a future Worker profile (parent spec) can be added without changing call sites.

### 3. Interim reference-data caching

- **3.1** Locations, Subsidiaries, and Vendors reads SHALL be cached per process via `IMemoryCache` (`Microsoft.Extensions.Caching.Memory` from local cache) with 6 h TTL, as decorators registered in the DI layer without modifying the underlying integrations.
- **3.2** Cache keys SHALL follow the parent D7 shape (`ns:cache:v{version}:{interface}:{method}:{hash(intent + employeeId where per-user)}`) so the later Redis swap is mechanical.
- **3.3** Transactional integrations SHALL receive no decorator.
- **3.4** Flush mechanism SHALL be process restart; this limitation SHALL be documented in the design (no cross-process flush in this phase).

### 4. Testing within constraints

- **4.1** Unit/integration tests SHALL live in the existing `Integration.NS.Tests` (xUnit).
- **4.2** `WireMock.Net` **1.7.4** (cached) SHALL serve as the fake NetSuite; no Testcontainers, no NBomber.
- **4.3** A hand-rolled harness (`Parallel.ForEachAsync` driving the service against the fake) SHALL verify the limiter cap and retry behavior under burst.

## Acceptance Criteria

- **AC1** WHEN 30 parallel writes execute against the fake NetSuite THEN it SHALL observe at most **2** simultaneous in-flight requests per app instance.
- **AC2** WHEN a write receives 429 THEN it SHALL be retried once with jitter and SHALL NOT surface an immediate user error.
- **AC3** WHEN a write times out or receives 5xx THEN it SHALL NOT be retried, the fake SHALL record exactly one attempted create, and the user SHALL see the existing parsed error.
- **AC4** WHEN a read receives 5xx or times out THEN it SHALL be retried once.
- **AC5** WHEN NetSuite returns a validation error THEN no retry SHALL occur and the message SHALL be byte-identical to today.
- **AC6** WHEN the same reference read occurs twice within 6 h in one process THEN the second read SHALL issue zero NetSuite calls.
- **AC7** WHEN transactional data is requested THEN it SHALL always come fresh from NetSuite.
- **AC8** WHEN the limiter env var is set to a new value THEN the cap SHALL change without code modification.

## In-Scope

- Unified limiter (§1), write-safe retry (§2), memory-cache decorators (§3), tests + harness (§4).
- Gating the (currently unused) OAuth1 path for correctness.

## Out-of-Scope

- Any database change: no tables, no migrations, no stored procedures (client constraint). This removes the durable ledger, background worker, status endpoint, and Trip Ticket async pilot — all remain in the parent spec pending an allowed store.
- Redis and any Redis-backed implementation; cross-process shared gate; true account-wide cap of 3.
- Testcontainers, NBomber, `Microsoft.Extensions.Caching.StackExchangeRedis`, Scrutor.
- NetSuite-side changes (restlet idempotency verification is moot while writes stay synchronous).
- Expanding retry beyond the Interactive profile.

## Preservation Requirements

- User-visible parsed error strings SHALL be preserved byte-identical.
- UI flows and page behavior SHALL NOT change (saves remain synchronous).
- Transactional reads SHALL remain uncached.
- NetSuite payload formats and restlet contracts SHALL NOT change.
- No secrets in committed configuration (limiter size via env var only, with safe default).
- The interim limiter SHALL be removed, not layered, when the parent spec's real gate lands.

## Resolved Decisions (from this discussion, 2026-10-07)

- **No database changes — client constraint (absolute).** Ledger/worker/status/pilot deferred to parent spec.
- **No Redis/Docker/downloads — environment constraint.**
- **Hot path is OAuth2 `MakeRequest`** (verified: all active writes + SuiteQL reads; OAuth1 has no active callers).
- **Write retries are 429-only** — the only duplicate-safe retry without a durable ledger.
- **Read vs. write classified by URL**, not HTTP method (SuiteQL reads use POST).
- **Limiter: single shared semaphore, default 2, env-configurable.**
- **Cache: per-process `IMemoryCache`, restart-to-flush.**
