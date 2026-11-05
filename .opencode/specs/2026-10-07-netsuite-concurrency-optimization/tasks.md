# Tasks — NetSuite Concurrency Optimization (WMS ↔ NetSuite)

```text
Date: 2026-10-07
Status: Confirmed
Mode: FEATURE
Spec: design.md (sibling document)
Goal: Cap WMS→NetSuite concurrency at 3 account-wide via a shared Redis lease gate, add safe retries, a durable idempotent write pipeline, and reference-data caching — per confirmed requirements and design.
```

## Global Constraints & Background

- **Build:** `dotnet build WMS.slnx` from solution root (requires .NET 9 SDK — hosts are net8.0/net9.0 mixed). Follow `CLAUDE.md`'s workflow: probe shell first, 3-attempt fix cap, `claude/feat/` branch, short imperative commits, stage specific files only. **Caution:** `CLAUDE.md` is stale (LSMS-v4; references nonexistent `.claude/` guides) — treat verified code as truth.
- **No secrets in committed config** — Redis connection string via environment variables only.
- **New projects** (`Integration.NS.Tests`, `Tests.LoadTesting`) must be added to `WMS.slnx`.
- **Preservation:** NetSuite payload formats, restlet scripts, NSScripts, mobile, and user-visible error strings unchanged except where a task explicitly says otherwise.
- **Test Redis:** Testcontainers if Docker available; else shared dev instance flushed between tests.

## Phase 0 — Prerequisites

**Task 1. Provision Redis.** Stand up the instance (recommended: Azure Cache for Redis Basic; alternatives per design D10). Store the connection string as an environment variable reachable by both hosts. *Acceptance:* both hosts connect and PING succeeds. _Requirements: 1.1–1.4, 7.1_

**Task 2. Verify test-Redis strategy.** Check Docker availability in dev/CI; pick Testcontainers vs. shared dev instance. *Acceptance:* a red/green connectivity test runs locally. _Requirements: 8.2_

**Task 3. Add packages.** `Integration.NS`: `StackExchange.Redis`, `Polly`. New test project: `xunit`, `WireMock.Net`, `Testcontainers.Redis` (if Task 2 chose it). *Acceptance:* solution builds. _Requirements: 1.1, 5.1_

## Phase 1 — Correctness Foundation

**Task 4. Create `Integration.NS.Tests`** (xUnit, references `Integration.NS`), added to `WMS.slnx`. *Acceptance:* empty fact runs green. _Requirements: 8.2_

**Task 5. Typed exceptions.** New `Integration.NS/Exceptions/`: `NetSuiteTransientException` (429/408/5xx/network), `NetSuiteValidationException` (4xx w/ detail), `NetSuiteGateUnavailableException`. Refactor throw sites `NetSuiteApiClientService.cs:326-330` and `:493-497` to classify by status code; **message = existing `BuildNetSuiteErrorMessage` output, unchanged**. *Acceptance:* unit tests classify each status; message strings byte-identical to current. _Requirements: 4.1, 4.2_

**Task 6. Token fixes.** Delete premature expiry write `NetSuiteApiClientService.cs:128`; cache imported RSA via `static Lazy<RSA>` replacing per-call file read `:164`; set explicit `_httpClient.Timeout` (100 s, env-configurable) at `:73`. *Acceptance:* unit tests — expiry set only after successful refresh; key file read exactly once; timeout applied. _Requirements: 3.1, 3.2, 3.3_

**Checkpoint A:** `dotnet build` green; all tests green; no caller-visible change except exception types.

## Phase 2 — Concurrency Gate

**Task 7. Implement the gate.** New `Integration.NS/Concurrency/`: `INetSuiteConcurrencyGate` (`AcquireAsync`, `ReleaseAsync`) + Redis lease implementation (sorted set `ns:gate:leases`, atomic Lua acquire: prune expired → `ZCARD < 3` → `ZADD(now+5min)`; release = `ZREM`). Bounded wait 45 s → `NetSuiteGateUnavailableException`. Include lightweight observability: log acquire/release and a max-in-flight gauge (Req 8.1). *Acceptance:* integration tests prove properties 1 (cap), 2 (lease expiry), 4 (fail-closed with stopped Redis). _Requirements: 1.1–1.6, 8.1_

**Task 8. Wire the gate into both request paths.** In `MakeRequest` (`:250-336`) and `MakeRequestOAuth1` (`:385-498`): remove `_requestSemaphore` (`:209-210`); acquire before send; **release in `finally` only when a response was received — on pre-response exception, abandon the lease** (TTL reclaims). *Acceptance:* integration test — 30 parallel calls, WireMock fake NetSuite never observes >3 simultaneous; forced caller timeout does not release (property 3). _Requirements: 1.1, 1.2, 1.5_

**Task 9. Unify login-time calls.** Remove `_netsuiteSemaphore` (`AuthenticationController.cs:29`) and its `WaitAsync/Release` at `:79+`; the underlying integration calls now flow through the gated service. *Acceptance:* login E2E regression passes. _Requirements: 2.2, 1.1_

**Task 10. Remove decorative semaphores.** Delete fields and `WaitAsync/Release` pairs in `TripTicketCVU.razor.cs:55,348,364,428,444`; `STRForm.razor.cs:61` (+ all usages); `ITRForm.razor.cs:50,92,102`; `SupplierReturnForm.razor.cs:52,104,114`. *Acceptance:* all four forms build; save flows unchanged. _Requirements: 2.1_

**Checkpoint B:** integration test suite green (cap ≤3 across both paths + login); all forms regression-pass.

## Phase 3 — Retry

**Task 11. Polly v8 profiles.** Two pipelines in `Integration.NS`: Interactive (1 retry, 5 s jittered) and Worker (5 s/15 s/45 s/2 m/5 m, jittered, 15-min total timeout). Optional `NetSuiteRetryProfile` parameter (default Interactive) on `MakeRequest`/`MakeRequestOAuth1`. `ShouldHandle`: transient → retry; validation/gate-unavailable → never. *Acceptance:* tests — scripted 429 retried with jitter; 400 never; gate-unavailable never. _Requirements: 5.1–5.4_

## Phase 4 — Write Pipeline

**Task 12. Ledger.** Entity `NsOperationLedger` in `Domain.Entities`; EF config + migration `[APHI.WMS]NsOperationLedger` in `Database.MsSql` (follow existing migration conventions); repository interface in `Application.UseCases/Repositories/Integration/Others/`, implementation in `Database.MsSql` (register alongside existing repositories — locate exact DI file during implementation). Columns/indices per design §4. *Acceptance:* tests — unique `(OperationType, IdempotencyKey)` rejects duplicates; state transitions Pending→Processing→Done/Failed. _Requirements: 6.1, 6.2_

**Task 13. Worker.** New `Api.CoreWebAPI/BackgroundServices/NsOperationWorker.cs` (`BackgroundService`): `sp_getapplock('NsOperationWorker')` → claim batch via `LockedUntil` lease → check-before-create → post with Worker profile → Done / Failed+LastError; stuck `Processing` rows requeue. Register in `Api.CoreWebAPI`. *Acceptance:* tests — two concurrent workers never take the same row (property 8); forced post-timeout creates exactly one record (property 6); enqueue accepted while Redis is down (property 7). _Requirements: 6.2–6.5_

**Task 14. Status endpoint.** New `Api.CoreWebAPI/Controllers/Operations/OperationStatusController.cs`: `GET api/operations/{idempotencyKey}`. *Acceptance:* test returns each lifecycle state. _Requirements: 6.4_

**Task 15. INVESTIGATION — NetSuite-side idempotency (D8).** With the NetSuite developer: confirm restlet 1862 (Trip Ticket) can check-before-create on the WMS transaction ID; confirm `ChangeShipStatus` PATCHes are naturally idempotent (status re-set). **Blocks pilot completion, not start.** *Acceptance:* written confirmation + restlet change scheduled, or documented fallback (natural-key check). _Requirements: 6.2_

**Task 16. Pilot — Trip Ticket save becomes queued.** Convert `PostTripTicketCmdHandler` (`Application.UseCases/Commands/Transaction/TripTicket/NS/PostTripTicketCmd.cs:11`) from synchronous `SaveTripTicket` to ledger enqueue (OperationType `TripTicketCreate`, IdempotencyKey = WMS trip ticket ID, PayloadJson = serialized `PostTripTicketDTO`), returning success "Received". Worker deserializes and calls `SaveTripTicket` with Worker profile. UI: `TripTicketCVU` shows Received → Done/Needs review via status endpoint. *Acceptance:* E2E test — save → ledger row → worker → fake NS → Done; forced timeout → no duplicate (AC2). _Requirements: 6.1–6.5, AC1, AC2_

**Checkpoint C:** pilot E2E green; regression — all non-pilot transaction types still save correctly (synchronous, through the gate).

## Phase 5 — Caching

**Task 17. Distributed cache registration.** `AddStackExchangeRedisCache` (env connection string) in both hosts. _Requirements: 7.1_

**Task 18. Cache decorators.** New `Integration.NS/Implementations/Others/Caching/`: manual decorators over `ILocationIntegration`, `ISubsidiaryIntegration`, `IVendorIntegration`; key `ns:cache:v{version}:{interface}:{method}:{hash(intent + employeeId where per-user, e.g. LocationIntegration.cs:83-97)}`; TTL 6 h; re-register wrapped instances in `NSServicesDI.cs` after `:18`. **No decorator on transactional integrations.** *Acceptance:* tests — second read within TTL issues zero NetSuite calls (property 9); flush invalidates; transactional reads always call. _Requirements: 7.1, 7.2_

**Task 19. Flush endpoint.** `POST api/operations/cache-flush` (admin-authorized) — `INCR ns:cache:version`. *Acceptance:* test — flush → next read hits NetSuite. _Requirements: 7.3_

## Phase 6 — Validation & Rollout

**Task 20. Load test.** New `Tests.LoadTesting` (NBomber console, in `WMS.slnx`): 30–50 concurrent users mixing saves + reference reads against the app with WireMock fake NetSuite. *Acceptance:* max observed concurrency ≤3; p95 save-to-ack within agreed target; zero duplicates. _Requirements: 8.1, 8.2, AC1_

**Task 21. Full regression.** Login with NS claims; all four forms; transactional reads fresh; cache behaviors. *Acceptance:* all green. _Requirements: preservation set_

**Task 22. Staging soak & rollout.** Deploy to staging; observe NetSuite Integration Governance page — peak ≤3 sustained across a busy window; then production. Expansion to other transaction types is a **follow-up spec**, not this one. _Requirements: 8.1, AC5, AC6_

## Optional (explicitly not MVP)

- **O1.** Dedicated worker project lift (from D4 seam) — when API scale-out makes single-drain a bottleneck.
- **O2.** Priority slot split (2 normal + 3 priority) — only if reads starve in production.
- **O3.** Items caching — same decorator pattern, later.
- **O4.** Fix `NSServicesDI.cs:14` / `NSImplementationDI.cs:58` double registration — separate cleanup, out of scope.
