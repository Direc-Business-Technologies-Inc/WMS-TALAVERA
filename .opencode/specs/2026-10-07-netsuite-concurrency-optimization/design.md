# Design — NetSuite Concurrency Optimization (WMS ↔ NetSuite)

```text
Date: 2026-10-07
Status: Confirmed
Mode: FEATURE
Scope: NetSuite outbound-call governance, retry, write queueing, and reference-data caching across Web.BlazorServer, Api.CoreWebAPI, and a new background worker
Related: None found (reference doc: .opencode/docs/WMS_NetSuite_Request_Flow.md; sibling: requirements.md)
```

## Decision Record

**Problem:** Two processes call NetSuite with uncoordinated limiters (observed peak 8 vs. account limit 5); OAuth1 is ungated; retries are absent; writes are synchronous and duplicate-prone; reference reads burn slots.

| # | Decision | Rationale | Trade-off accepted |
|---|---|---|---|
| D1 | **Custom Redis lease gate** (small Lua script on StackExchange.Redis) in `Integration.NS`, not the `RedisRateLimiting` package | Requirements 1.5/1.6 (release-on-response only; TTL leases for crashed holders) are hard semantic requirements the package doesn't guarantee | ~60 lines of Lua we own and test vs. a dependency whose internals we'd have to audit anyway |
| D2 | **Single pool, 3 permits** (priority split deferred) | Matches flow doc; leaves 2 of 5 free; no evidence yet of read starvation | If reads get starved later, add a second pool — the gate interface supports it |
| D3 | **Fail-closed, bounded wait 45 s**, then typed `NetSuiteGateUnavailableException` | No ungated call ever reaches NetSuite; 45 s sits in the confirmed 30–60 s window | During Redis outage, interactive reads surface "system busy" — accepted per decision 2026-10-07 |
| D4 | **Worker lives in `Api.CoreWebAPI`**, guarded by `sp_getapplock`, built as a liftable `BackgroundService` | Keeps background work out of the Blazor UI process; API is already the integration edge; status endpoint lives there anyway | If the API scales out, the app-lock serializes drainers (correct but single-drainer); lift to a dedicated project if that becomes a bottleneck |
| D5 | **SQL operation ledger as the queue** (ASB deferred) | Ledger is needed for idempotency/status regardless; it doubles as the queue; zero new infrastructure | Polling-based drain (fine at WMS volumes); no ASB dead-lettering — Failed status + max attempts plays that role |
| D6 | **Polly v8, two retry profiles** — Interactive (default: 1 retry, ~5 s) and Worker (5 s/15 s/45 s/2 m/5 m, jittered, 15-min budget) — selected by explicit argument, not ambient context | Explicit selection is testable and auditable; long budget only exists where a human isn't waiting | `MakeRequest`/`MakeRequestOAuth1` gain an optional profile parameter (contract change, internal only) |
| D7 | **Cache via `IDistributedCache` (Redis) decorators** on `ILocationIntegration`, `ISubsidiaryIntegration`, `IVendorIntegration`; key = method + serialized `DataGridIntent` + employee scope where applicable; 6 h TTL; flush via **key-version bump** | One Redis serves gate + cache; decorators leave integrations untouched; version-bump flush avoids SCAN/DEL races | Per-user methods (`GetCurrentUserAllowedLocations`) cache per employee — access changes lag up to 6 h; flush covers it |
| D8 | **Idempotency: WMS transaction ID stamped in payload + check-before-create** | Matches flow doc's implementation notes and Req 6.2 | Restlet scripts (1853/1862/1878) are theirs and can check; **record-API transforms need NetSuite-side verification** — investigation task, not assumption |
| D9 | **Pilot-first write conversion:** queue-ify **Trip Tickets** first, expand to other transaction types after | Converting every synchronous save at once is the riskiest change in the feature; pilot contains the blast radius | Other transaction types keep synchronous saves (through the gate) until their phase ships |
| D10 | **Redis provisioning:** Azure Cache for Redis (Basic tier) recommended; alternatives below | Team is Azure-oriented (ASB was on the table); managed, backed up, no VM to tend | Cost (~low double digits/month); dev/test uses Testcontainers or a local instance |

**Redis hosting options considered:** (a) Azure Cache for Redis — managed, recommended; (b) Redis on an existing VM — no new cost, but you own uptime/patching; (c) containerized — dev/test only. Final pick is an environment decision at implementation time; the design works identically against any Redis ≥ 6.

## What Already Exists (verified)

| Component | Evidence | Role in design |
|---|---|---|
| OAuth2 request path | `NetSuiteApiClientService.cs:250-336` (`MakeRequest`), gated by `_requestSemaphore` `:210` | Gate replaces semaphore; gains retry profile + typed exceptions |
| OAuth1 request path | `NetSuiteApiClientService.cs:385-498` (`MakeRequestOAuth1`), **ungated** | Same wrapping as OAuth2 — closes the hole |
| Token management | `:122-155` (`GetAccessToken`, premature 30-min write at `:128`), `:338-377` (`GetValidAccessToken`, double-check lock — good pattern, kept), `:157-202` (`GetJwtToken`, key file re-read at `:164`) | Fixes per Req 3; semaphore-based refresh retained |
| Static HttpClient | `:73`, no explicit timeout | Gets explicit timeout (100 s, env-configurable) |
| Error parsing/logging | `:500-654` (`GetNetSuiteErrorDetail`, `BuildNetSuiteErrorMessage`, `LogNonSuccessResponse`) | Reused unchanged; output strings become exception messages (preserves user-visible detail) |
| Login-time NS calls | `AuthenticationController.cs:29` semaphore, `:79` usage | Semaphore removed; calls flow through the gate |
| Decorative form semaphores | `TripTicketCVU.razor.cs:55`, `STRForm.razor.cs:61`, `ITRForm.razor.cs:50`, `SupplierReturnForm.razor.cs:52` | Deleted |
| DI registrations | `NSServicesDI.cs:14` (AddScoped), `NSImplementationDI.cs:58` (TryAddTransient — no-op, already registered; noted, not fixed — out of scope) | Gate, decorators, and Polly pipelines registered here |
| Hosts | `Web.BlazorServer/Program.cs:23`, `Api.CoreWebAPI/Registers/WebApplicationService.cs:22` | Both get gate + cache wiring; API additionally gets worker + status endpoint |
| EF migrations | `Database.MsSql/Migrations/` (`[APHI.WMS]` naming convention) | Ledger migration follows this convention |
| Reference integrations | `LocationIntegration.cs:23-185` — methods keyed by `DataGridIntent`, one per-user method (`:83-97`) | Decorator targets; key shape per D7 |
| SuiteQL scripts | `Integration.NS/NSScripts/*.sql` via `ISqlQueryManager` | Unchanged |
| Mobile | `Mobile.MAUI.csproj:107-108` — API-only, no NS reference | Unchanged |

**What changes:** `NetSuiteApiClientService` internals, DI wiring, login controller, four form files (semaphore removal), new gate/exceptions/retry/ledger/worker/cache/status components, one EF migration, new test projects. **What doesn't:** NetSuite payload formats (except idempotency key per D8, pending verification), restlet scripts (except check-before-create, pending verification), NSScripts, mobile, existing UI flows (except pilot's save becomes asynchronous).

## Architecture Overview

```mermaid
flowchart TB
    subgraph Clients
        B["Web.BlazorServer<br/>(warehouse UI)"]
        M["Mobile.MAUI<br/>(via API only)"]
    end
    subgraph API["Api.CoreWebAPI"]
        EP["Controllers<br/>(saves, status, cache-flush)"]
        W["NsOperationWorker<br/>(BackgroundService,<br/>sp_getapplock guarded)"]
    end
    subgraph NS["Integration.NS"]
        G["NetSuiteConcurrencyGate<br/>(Redis leases, 3 permits)"]
        C["Cache decorators<br/>(Loc/Sub/Vendor, 6h TTL)"]
        S["NetSuiteApiClientService<br/>(typed exceptions,<br/>Polly v8 profiles)"]
    end
    L[("SQL Server<br/>NsOperationLedger")]
    R[("Redis<br/>gate leases + cache")]
    N[("NetSuite<br/>limit 5")]

    B -->|reads| C
    B -->|save (pilot: TripTicket)| L
    M --> EP
    EP -->|enqueue| L
    EP -->|reads| C
    W -->|drain| L
    W -->|post writes| S
    B -->|interactive reads/writes| S
    C --> R
    G --> R
    S --> G
    G -->|max 3 in flight| N
```

## Components & Contracts

**1. `INetSuiteConcurrencyGate`** (new, `Integration.NS/Concurrency/`)
- `Task<GateLease> AcquireAsync(CancellationToken ct)` — bounded wait 45 s; throws `NetSuiteGateUnavailableException` on timeout/backend-down.
- `Task ReleaseAsync(GateLease lease)` — removes the lease.
- Redis structure: sorted set `ns:gate:leases`, members = lease IDs (GUIDs), scores = expiry epoch. Acquire (Lua, atomic): prune entries with score < now → if `ZCARD < 3` → `ZADD(now + leaseTtl)` → return 1; else 0. Release: `ZREM`. **Lease TTL = 5 min** (> slowest expected call; NetSuite server cutoff is 15 min — see Accepted Consequences).
- **Release-on-response semantics (Req 1.5):** the lease is released in the `finally` only when a response was received. On caller timeout/exception *before* response, the lease is **abandoned** — the TTL reclaims it. This is what prevents over-admission while NetSuite still processes an abandoned call.

**2. Typed exceptions** (new, `Integration.NS/Exceptions/`)
- `NetSuiteTransientException` — HTTP 429/408/5xx, network errors. Message = existing `BuildNetSuiteErrorMessage` output (preserved detail).
- `NetSuiteValidationException` — 4xx with parsed detail.
- `NetSuiteGateUnavailableException` — fail-closed trip.

**3. Retry profiles** (Polly v8, package added to `Integration.NS`)
- `MakeRequest`/`MakeRequestOAuth1` gain optional `NetSuiteRetryProfile profile = Interactive`.
- Interactive: 1 retry, 5 s jittered. Worker: 5 retries — 5 s/15 s/45 s/2 m/5 m, jittered, 15-min total timeout wrapping the pipeline.
- `ShouldHandle`: transient exception → retry; validation/gate-unavailable → never.

**4. `NsOperationLedger`** (entity in `Domain.Entities`, EF config + migration `[APHI.WMS]NsOperationLedger` in `Database.MsSql`)
- Columns: `Id (Guid, PK)`, `OperationType (string)`, `IdempotencyKey (string)`, `PayloadJson (nvarchar(max))`, `Status (Pending|Processing|Done|Failed)`, `Attempts (int)`, `LockedUntil (datetime2, null)`, `LastError (nvarchar(max), null)`, `CreatedAt`, `UpdatedAt`.
- Unique index `(OperationType, IdempotencyKey)` — duplicate enqueue of the same logical operation is rejected at the DB.
- Index `(Status, LockedUntil, CreatedAt)` — the drain query.

**5. `NsOperationWorker`** (new, `Api.CoreWebAPI/BackgroundServices/`)
- Loop: `sp_getapplock('NsOperationWorker')` → claim batch (`Pending`, `LockedUntil` lease to survive worker crash) → per item: check-before-create (per D8) → post via `MakeRequestOAuth1/MakeRequest` with `Worker` profile → `Done`; on validation failure → `Failed` + `LastError`; on exhausted retries → `Failed` + `LastError` (dead-letter equivalent).
- Stuck recovery: `Processing`/`LockedUntil` past a threshold → requeued automatically.

**6. Status & flush endpoints** (new, `Api.CoreWebAPI/Controllers/Operations/`)
- `GET api/operations/{idempotencyKey}` — per-operation state (drives the UI's "Received → Done / Needs review" display).
- `POST api/operations/cache-flush` — bumps `ns:cache:version` (D7); admin-authorized.

**7. Cache decorators** (new, `Integration.NS/Implementations/Others/Caching/`)
- Manual decorator classes over `ILocationIntegration`, `ISubsidiaryIntegration`, `IVendorIntegration` (no Scrutor — fewer dependencies).
- Key: `ns:cache:v{version}:{interface}:{method}:{hash(intent + employeeId where per-user)}`; TTL 6 h; serialization via System.Text.Json.
- Transactional integrations (inventory, fulfillments, orders) get **no decorator** — Req 7.2.

**8. Token fixes** (in `NetSuiteApiClientService`)
- Delete `:128`; `static Lazy<RSA>` for the imported key; explicit `_httpClient.Timeout = 100 s` (env-configurable).

## Correctness Properties

1. **Cap:** under N concurrent callers (N ≫ 3), the number of simultaneously in-flight NetSuite requests never exceeds 3. *(Req 1.1, 1.2)*
2. **Lease expiry:** a lease held by a process that dies becomes acquirable by others after the lease TTL, without manual intervention. *(Req 1.6)*
3. **No early release:** a caller-side timeout does not release the permit; the cap holds while NetSuite still processes the abandoned call. *(Req 1.5)*
4. **Fail-closed:** with Redis unreachable, zero NetSuite requests are issued; callers receive `NetSuiteGateUnavailableException` within the bounded wait. *(Req 1.4)*
5. **Retry routing:** a 429 is retried with jitter; a 400 with validation detail is never retried. *(Req 4.1, 5.1)*
6. **Idempotency:** a worker retry after a post-timeout produces exactly one NetSuite record (check-before-create). *(Req 6.2)*
7. **Enqueue durability:** a save is accepted into the ledger even while the gate is unavailable. *(Req 6.5)*
8. **Single drain:** two concurrent worker instances never process the same ledger row. *(Req 6.3)*
9. **Cache hit/miss discipline:** a second identical reference read within TTL issues no NetSuite call; every transactional read does. *(Req 7.1, 7.2)*

## Error Handling & User-Visible Behavior

- **Save (pilot):** instant "Received" → background "Done" or "Needs review" with the same parsed detail strings users see today.
- **Gate unavailable:** "System busy — please try again" (reads); saves still enqueue (delayed, never lost).
- **Validation failure:** surfaced immediately, no retry, existing message content.
- **Worker dead-letter:** `Failed` + `LastError`, visible via status endpoint for operations to resolve.

## Testing Strategy

- **New projects** (confirmed; none exist today): `Integration.NS.Tests` (xUnit + **WireMock.Net** fake NetSuite + Redis via **Testcontainers** — falls back to a shared dev Redis if Docker is unavailable in CI) and `Tests.LoadTesting` (**NBomber** console driving the real app against the fake).
- **Unit:** gate Lua semantics (cap, expiry, release-on-response), retry predicates, cache keying, ledger state transitions.
- **Integration (the confidence core):** properties 1–9 asserted deterministically — e.g., 30 parallel saves against WireMock with a concurrency counter; forced timeouts; scripted 429/validation responses.
- **Load (NBomber):** 30–50 concurrent users; measures p95 save-to-ack and confirms the cap under realistic ramp — *not* relied upon for correctness.
- **Regression:** existing flows (login with NS claims, all four forms post-gate, transactional reads) verified unchanged.

**Property-based testing assessment: NOT APPLICABLE.** The critical properties are concurrency/protocol invariants, best verified with deterministic parallel integration tests against scripted responses; there is no natural data algebra to generate over.

## Accepted Consequences

- A caller timeout pins a permit for up to 5 min (lease TTL). With a 3-permit pool and 100 s HTTP timeout, worst case briefly reduces throughput — accepted in exchange for never over-admitting NetSuite.
- Per-user cached location access can lag up to 6 h; the flush endpoint mitigates.
- Non-pilot transaction types remain synchronous (through the gate) until their phase ships.
- `sp_getapplock` serializes drainers — one active worker regardless of API instance count.
- Idempotency for record-API transforms depends on NetSuite-side verification (D8) — if a record type can't carry the WMS ID, that operation type stays on check-by-natural-key or remains synchronous until resolved.

## Out-of-Scope (restated)

ASB; priority slot split; routing Blazor through the API; SuiteCloud Plus licensing; mobile changes; NetSuite script changes beyond D8's verified check-before-create; Items caching; fixing the `NSServicesDI`/`NSImplementationDI` double registration (noted as a separate cleanup).

## Verification Checklist

☐ Gate cap ≤ 3 proven in integration test ☐ Lease expiry test ☐ Fail-closed test ☐ Retry routing test ☐ Idempotency (forced-timeout) test ☐ Single-drain test ☐ Cache hit/miss test ☐ Pilot save → Received → Done E2E ☐ Load run: p95 + cap under load ☐ Regression: login, four forms, transactional reads ☐ Redis provisioned + wired via env config (no committed secrets)
