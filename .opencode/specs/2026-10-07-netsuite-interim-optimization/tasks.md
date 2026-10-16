# Tasks — NetSuite Interim Code-Only Optimization (WMS ↔ NetSuite)

```text
Date: 2026-10-07
Status: Confirmed
Mode: FEATURE
Spec: design.md (sibling document)
Goal: Cap per-process NetSuite concurrency at 2 (worst case 4 account-wide ≤ limit 5), add write-safe retries, and cache reference data per process — code-only, no DB, no downloads beyond the local NuGet cache.
```

## Global Constraints & Background

- **Build:** `dotnet build WMS.slnx` from solution root (verified green 2026-10-07, 0 errors). **Test:** `dotnet test Integration.NS.Tests`.
- **Offline:** restore resolves from the local NuGet cache — pin **exactly** `Polly` 8.4.2, `WireMock.Net` 1.7.4, `Microsoft.Extensions.Caching.Memory` 9.0.11 (matches the repo's EF Core 9.0.11-on-net8.0 precedent). Do not substitute versions.
- **No database changes of any kind** — client constraint. No tables, migrations, or stored procedures.
- **Branch/hygiene:** per `CLAUDE.md`, work on `claude/feat/<name>`, stage specific files only, short imperative commits. (`CLAUDE.md` is partially stale — the `.claude/` guides do exist; treat verified code as truth.)
- **Preservation:** user-visible error strings byte-identical; `INetSuiteApiClientService` signatures stay source-compatible (new parameters optional); no secrets in config (env vars only).
- **Hosts assumption:** exactly two NetSuite-calling processes (`Web.BlazorServer`, `Api.CoreWebAPI`). A third host requires lowering the limiter env var.

## Phase 0 — Packages

**Task 1. Add cached package references.**
- `Integration.NS/Integration.NS.csproj`: add `Polly` **8.4.2** and `Microsoft.Extensions.Caching.Memory` **9.0.11**.
- `Integration.NS.Tests/Integration.NS.Tests.csproj`: add `WireMock.Net` **1.7.4**.
- *Acceptance:* `dotnet restore` + `dotnet build` green with network access denied. _Requirements: 2.2, 3.1, 4.2_

## Phase 1 — Resilience Foundation

**Task 2. `StatusCode` on transient exceptions.**
- `Integration.NS/Exceptions/NetSuiteTransientException.cs`: add `HttpStatusCode? StatusCode { get; }` via new optional constructor param (existing constructors preserved).
- `NetSuiteExceptionFactory.cs:41-42` (`CreateForStatus`): populate it; `CreateForNetworkError` (`:48-52`) leaves null.
- *Acceptance:* factory unit tests (`Integration.NS.Tests/Exceptions/NetSuiteExceptionFactoryTests.cs`) assert `StatusCode` per status; messages still byte-identical. _Requirements: 2.1, 2.3_

**Task 3. Unified limiter.**
- `NetSuiteApiClientService.cs:253`: replace `new(4, 4)` with size from new `internal static int GetMaxInFlightPerProcess()` — env `NETSUITE_MAX_INFLIGHT_PER_PROCESS`, fallback 2, minimum 1 (mirror `GetHttpTimeoutSeconds` at `:94-98`).
- `MakeRequestOAuth1` (`:455-570`): `await _requestSemaphore.WaitAsync()` immediately before the send section (`:534`), `Release()` in a new `finally`. Token refresh (`GetValidAccessToken`, called at `:298`) stays outside the limiter in both paths.
- *Acceptance:* both paths share one static semaphore; `dotnet build` green. _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

**Task 4. Resilience components** — new folder `Integration.NS/Resilience/`:
- `NetSuiteRequestKind.cs` — `enum { Read, Write }`.
- `NetSuiteRetryProfile.cs` — `enum { Interactive }` (comment: Worker reserved for parent spec).
- `NetSuiteRequestClassifier.cs` — `Classify(string url, HttpMethod method)`: GET, or URL containing `/query/v1/` (ordinal-ignore-case) → `Read`; else `Write`.
- `NetSuitePipelines.cs` — two cached `ResiliencePipeline`s: `InteractiveRead` (ShouldHandle: any `NetSuiteTransientException`) and `InteractiveWrite` (ShouldHandle: `StatusCode == 429` only). Both: `MaxRetryAttempts = 1`, `Delay = 5 s`, `Constant` backoff, `UseJitter = true`.
- *Acceptance:* compiles; classifier + predicate unit tests green. _Requirements: 2.2, 2.4, 2.5, 2.6_

**Task 5. Wire retry into both request paths.**
- `MakeRequest` (`:293`): signature gains `NetSuiteRequestKind? kind = null, NetSuiteRetryProfile profile = NetSuiteRetryProfile.Interactive`; `kind ??= Classify(url, method)`. Extract the existing send/handle body (`:305-375`) into a local `SendAndHandle`; structure: acquire → `try { return await pipeline.ExecuteAsync(_ => SendAndHandle(), ct); } finally { Release(); }` — retry executes **inside** the permit hold (D5).
- `MakeRequestOAuth1` (`:455`): same optional params and wrapping around its send section.
- *Acceptance:* existing call sites compile unchanged; error strings unchanged. _Requirements: 2.1–2.6, 1.4_

**Task 6. Test seams** (internal only, guarded by existing `InternalsVisibleTo`, csproj `:66-68`):
- `internal static void OverrideHttpClientForTests(HttpClient client)` (replaces `_httpClient` use at `:75`).
- `internal static void OverrideUrlRootsForTests(...)` — requires changing restlet URL fields (`:58-62`) from `static readonly` to `static` with a reset method.
- *Acceptance:* seams usable from tests; production behavior untouched. _Requirements: 4.1, 4.2_

**Checkpoint A:** `dotnet build` + existing `Integration.NS.Tests` green; no observable behavior change except the limiter size (4→2).

## Phase 2 — Behavioral Tests (Retry + Limiter)

**Task 7. Unit tests** (`Integration.NS.Tests`): classifier matrix (SuiteQL-POST→Read, record-POST→Write, GET→Read, restlet-URL→Write, override wins); write predicate admits 429, rejects 500/408/network; read predicate admits all transient; env parsing (valid/invalid/unset→2).
_Requirements: 1.3, 2.3, 2.4, 2.5_

**Task 8. Integration tests** (WireMock 1.7.4 via Task 6 seams):
- Cap: 30 parallel mixed calls against a 100 ms-latent fake with a concurrency counter → max in-flight ≤ 2 (properties 1, 2).
- Write: scripted 429→200 → exactly 2 attempts (property 3); scripted 500 and forced `TaskCanceledException` → exactly 1 attempt, `NetSuiteTransientException` (property 4); timeout → subsequent call proceeds (property 8).
- Read: scripted 500→200 → exactly 2 attempts (property 5).
- Validation: 400 → 1 attempt, `NetSuiteValidationException`, message byte-identical (property 6).
- *Acceptance:* properties 1–8, 10 green. _Requirements: 1.1–1.3, 1.5, 2.1–2.5, 4.2_

**Checkpoint B:** full test suite green.

## Phase 3 — Caching

**Task 9. Decorators** — new folder `Integration.NS/Implementations/Others/Caching/`:
- `NsCacheKey` helper: `ns:cache:v1:{Interface}:{Method}:{SHA256(json(intent) + scalarParams + employeeScope)}` (System.Text.Json + SHA256).
- `CachedLocationIntegration` (8 methods — per-user `:14-15` scoped by `employeeId` param or the `com.direcbusiness.wms.nsEmployeeId` claim via `IHttpContextAccessor`; **claim absent → skip cache**), `CachedSubsidiaryIntegration` (5 methods), `CachedVendorIntegration` (6 methods). Hit → return; miss → inner call, store 6 h absolute expiry.
- *Acceptance:* key determinism/discrimination unit tests green. _Requirements: 3.1, 3.2, 3.4_

**Task 10. DI wiring.**
- `NSImplementationDI.cs:45-47`: replace the three `TryAddTransient<I…, Impl>()` lines with concrete registrations + decorated interface factories (`ILocationIntegration` → `new CachedLocationIntegration(inner, cache, accessor)`, etc.).
- `NSServicesDI.cs` (before `:18`): `services.AddMemoryCache()`.
- **No decorator on any transactional integration** (verified list at `NSImplementationDI.cs:32-56` — untouched).
- *Acceptance:* both hosts build; decorators resolve. _Requirements: 3.1, 3.3_

**Task 11. Cache integration tests.** Second identical read → zero NetSuite calls; differing intent/employeeId → separate entries; no-claim per-user call → always calls through; a transactional integration (e.g., `IInventoryIntegration`) → always calls through. _Requirements: 3.1–3.3, 4.1_

**Checkpoint C:** property 9 green; full suite green.

## Phase 4 — Validation & Rollout

**Task 12. Load harness** — xUnit test with `[Trait("Category","Load")]` (excluded from default `dotnet test` via filter): 30 parallel posts, 100 ms fake latency; asserts cap ≤ 2 per instance and exact attempt counts (zero duplicate writes). _Requirements: 4.3, AC1–AC3_

**Task 13. Full regression.** Login with NS claims; the four forms (Trip Ticket, STR, ITR, Supplier Return) save flows; reference grids load; transactional reads fresh; `dotnet build` + `dotnet test` green. _Requirements: preservation set, AC5–AC7_

**Task 14. Rollout notes** (in implementation notes, not code): document `NETSUITE_MAX_INFLIGHT_PER_PROCESS` (default 2, needs restart), the two-host assumption, restart-to-flush cache behavior, and rollback path (env var back to 4 / revert PR). _Requirements: 1.3, 3.4_

## Optional (explicitly not MVP)

- **O1.** Env-configurable cache TTL (`NETSUITE_CACHE_TTL_HOURS`, default 6).
- **O2.** Max-in-flight logging counter (parent Req 8.1) — `Interlocked` high-water mark logged on release; cheap production evidence for the cap.
- **O3.** Items caching via the same decorator pattern (parent O3).
