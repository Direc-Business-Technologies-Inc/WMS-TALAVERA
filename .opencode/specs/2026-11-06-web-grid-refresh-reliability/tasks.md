# Tasks — Web Grid Refresh Reliability and NetSuite Capacity Optimization

- **Date:** 2026-11-06
- **Status:** Confirmed
- **Mode:** BUGFIX
- **Baseline reviewed:** `2671287d`
- **Scope:** Packing/Receiving grids, shared loading coordination, read freshness, cancellation/coalescing, and NetSuite diagnostics.
- **Spec:** [Design](design.md)
- **Implementation:** [Code delivery, verification and pending hosted work](implementation.md)
- **Requirements:** [Requirements](requirements.md)
- **Related:**
  - [Interim NetSuite optimization](../2026-10-07-netsuite-interim-optimization/tasks.md)
  - [Web data-fetch optimization](../2026-10-08-web-fetch-optimization/tasks.md)
  - [Packing/receiving performance](../../../docs/packing-receiving-performance.md)

## Goal

Make refresh behavior correct and predictable, reduce avoidable NetSuite reads, and identify the remaining loading bottleneck without increasing the five-slot account limit or weakening transaction safeguards.

## Technical Context and Global Constraints

- Web and NetSuite integration target .NET 8; API targets .NET 9.
- Use existing `Web.BlazorServer.Tests` and `Integration.NS.Tests`.
- Preserve component → feature handler → MediatR → integration boundaries, documented async conventions, and DTO/ViewModel separation.
- No database migration, public endpoint, Redis deployment, or SAP/mobile changes.
- **No auto-fulfillment investigation, optimization, disabling, or removal in this patch.**
- Preserve fulfillment validation, payloads, write retries, and uncertain-outcome handling.
- Extend existing abstractions rather than introducing a new general-purpose data framework or remediating unrelated architectural debts.
- References below describe the reviewed baseline; re-check current source and applicable repository instructions before implementation.
- Run builds/tests sequentially where projects share output dependencies. Verify shell availability before build according to applicable project guidance.
- Do not put secrets in committed configuration or perform live load tests without separate authorization.
- Confirmed planning documents do not represent implementation or verification completion. Checkboxes below remain pending.

## Phase A — Baseline and Defect Characterization

### Task 1. Record the deployment and performance baseline

- [ ] Record deployed web/API build identifiers.
- [ ] Inventory NetSuite-calling processes, IIS worker configuration, deployment overlap, effective limiter settings, and known other account consumers.
- [ ] Capture comparable Packing/Receiving first visits after idle/recycle, warm visits, explicit Sync, rapid filter/status changes, and representative concurrent usage.
- [ ] Separate permit wait, HTTP duration/attempts, total records duration, and click-to-visible-rows.

**Relevant evidence:**

- `Integration.NS/Services/NetSuiteApiClientService.cs:125–133,438–495`
- `docs/packing-receiving-performance.md:204–232`

**Completion criteria:** Baseline includes permissions, subsidiary, filters, page size, build, and timing phases. Combined process budgets/headroom are documented; unknown account traffic is identified rather than assumed absent. No auto-fulfillment-specific investigation is scheduled.

_Requirements: 6.1–6.5._

### Task 2. Add deterministic characterization tests

Create focused tests in existing test projects. Confirm final test-file locations during implementation rather than adding another project.

- [ ] Reproduce an older response completing after a newer request.
- [ ] Reproduce a queued refresh returning before its work finishes.
- [ ] Reproduce direct reload overlapping a Radzen load.
- [ ] Reproduce explicit Receiving Sync reusing completed cache entries.
- [ ] Reproduce failed records refresh becoming empty success.
- [ ] Reproduce failed badge refresh leaving unmarked totals.
- [ ] Characterize redundant Packing status-selection badge reads.

Use `TaskCompletionSource` or equivalent barriers to force overlap.

**Relevant implementation:**

- `Web.BlazorServer/Components/Shared/Abstraction/AppDataGrid.razor.cs:111–269`
- `Integration.NS/Implementations/Others/Caching/CachedReceivingIntegration.cs:20–38`
- `Web.BlazorServer/Components/Custom/PrintStatusView.razor:52–105`
- `Web.BlazorServer/Components/Pages/Transaction/Packing/STR/Component/STRPackingDataGrid.razor.cs:52–62,132–133`

**Completion criteria:** Applicable tests fail on unfixed behavior for the intended reason, without arbitrary sleeps or live NetSuite. Record any reproduction limitation.

_Requirements: 1.1–1.4, 2.1, 3.1–3.4, 4.1, 8.1–8.2._

**Checkpoint A:** Review failing cases and baseline evidence before changing shared behavior.

## Phase B — Correct Grid Coordination and Outcomes

### Task 3. Define internal read policy and refresh outcomes

- [ ] Introduce internal read policy `Normal` / `Fresh`.
- [ ] Introduce refresh outcomes `Succeeded`, `Failed`, `Cancelled`, `Superseded`, `Busy`.
- [ ] Investigate existing owning types/namespaces before choosing new file locations.
- [ ] Define independent request snapshots, scope equality, caller completion, and error metadata for retry feedback.
- [ ] Define successful-empty semantics separately from failure.

**Completion criteria:** Contracts distinguish failures from zero-row success. Policy does not use random cache keys/fake filters. No public API or database schema change.

_Requirements: 1.2–1.3, 2.1, 3.1–3.5, 5.3._

### Task 4. Unify shared-grid loading

Update:

- `Web.BlazorServer/Components/Shared/Abstraction/AppDataGrid.razor.cs:111–269`
- `Web.BlazorServer/Components/Custom/QuickDataGrid.razor:19`
- `Web.BlazorServer/Components/Custom/QuickDataGrid.razor.cs:107–111`

- [ ] Route Radzen callbacks and both direct reload overloads through one coordinator.
- [ ] Implement one active load and latest-pending request only.
- [ ] Capture independent inputs and gate publication by generation.
- [ ] Complete superseded callers explicitly; protect disposal.
- [ ] Preserve each entry point's intended paging/filter behavior.

**Completion criteria:** Older responses cannot overwrite current data; all callers receive accurate terminal outcomes; repeated actions do not create unbounded queues; saved paging/settings and selection tests remain valid.

_Requirements: 1.1–1.4, 8.2–8.3._

### Task 5. Stop publishing failed reads as empty datasets

Starting points:

- `Web.BlazorServer/Components/Pages/Transaction/Packing/STR/Component/STRPackingDataGrid.razor.cs:94–133`
- `Web.BlazorServer/Components/Pages/Transaction/Receiving/Components/TransferOrderGrid.razor.cs:103–134`
- `Web.BlazorServer/Services/Implementation/AppActionFactory.cs:48–60`

- [ ] Locate and apply equivalent corrections to Packing Returns/VRA, Receiving PO/Returns, and affected shared consumers.
- [ ] Retain same-scope successful rows with stale/error feedback.
- [ ] Prevent prior-scope rows appearing current after changed-scope failure.
- [ ] Distinguish initial failure, cancellation, and successful empty data.
- [ ] Return refresh outcomes visible to post-print callers.
- [ ] Preserve successful-write/failed-reconciliation messaging.

**Completion criteria:** A 429 cannot become "No records" success. Old rows do not masquerade as matching new scopes. Retrying reconciliation does not repeat the mutation.

_Requirements: 3.1–3.5, 8.3._

**Checkpoint B:** Ordering, completion, and failure-state tests pass before cache behavior changes.

## Phase C — Freshness, Coalescing, and Request Reduction

### Task 6. Implement fresh-read semantics through existing boundaries

Thread policy through touched feature handlers, MediatR queries, integration interfaces, and cache decorators. Locate remaining owning signatures before changing them.

Starting points:

- `Web.BlazorServer/Components/Pages/Transaction/Receiving/Components/TransferOrderGrid.razor.cs:64–71`
- `Integration.NS/Implementations/Others/Caching/CachedReceivingIntegration.cs:20–38`
- `Integration.NS/Implementations/Others/Caching/GridResultCache.cs:150–206`

- [ ] Retain normal caching for ordinary loads.
- [ ] Bypass completed entries for explicit Sync; allow fresh results to populate normal cache.
- [ ] Preserve mutation-generation boundaries.
- [ ] Avoid global eviction of unrelated users' entries on each Sync.

**Completion criteria:** Sync within TTL requests fresh data; normal navigation can still hit cache; post-mutation reconciliation cannot join older pre-mutation reads.

_Requirements: 2.1–2.3, 4.4._

### Task 7. Add safe in-flight coalescing

Starting points:

- `Integration.NS/Implementations/Others/Caching/GridResultCache.cs:150–206,214–263`
- `Integration.NS/Implementations/NSImplementationDI.cs:97–141`

- [ ] Extend integration read boundaries with bounded-lifecycle in-flight entries.
- [ ] Share success and failure and remove entries after completion.
- [ ] Implement independent waiter cancellation; cancel shared work only when no valid callers remain.
- [ ] Separate incompatible normal/fresh and mutation-generation requests.
- [ ] Preserve identity/subsidiary/authorization and full intent boundaries.
- [ ] Do not introduce a Packing completed-result TTL.

**Completion criteria:** Forced overlapping equivalent reads execute one upstream operation. Shared failure does not trigger a serial request per waiter. One cancelled waiter does not break another. Identity isolation and mutation invalidation remain correct.

_Requirements: 4.3–4.5, 8.2._

### Task 8. Remove redundant Packing badge reads and expose stale totals

Update:

- `Web.BlazorServer/Components/Pages/Transaction/Packing/STR/Component/STRPackingDataGrid.razor.cs:33–62`
- Equivalent Returns/VRA grid handlers, located before editing.
- `Web.BlazorServer/Components/Custom/PrintStatusView.razor:74–103`

- [ ] Do not refresh global totals solely because print-status selection changed.
- [ ] Retain initial, explicit Sync, mutation, and badge-scope refresh triggers.
- [ ] Retain last successful totals during reconciliation.
- [ ] Mark totals stale on failure and expose retry feedback.

**Completion criteria:** Status selection alone issues no badge query. Required reconciliation still updates rows/badges. Badge failure is visible and clears after successful retry.

_Requirements: 3.4–3.5, 4.1–4.2._

**Checkpoint C:** Freshness, true overlap, scope isolation, and request-count tests pass.

## Phase D — Cancellation and Bounded Admission

### Task 9. Propagate read cancellation end to end

- [ ] Update touched shared-grid, feature-handler, MediatR, integration/cache, paging, and transport contracts.
- [ ] Start with `Integration.NS/Implementations/Transactions/Packing/StockTransferRequestPackingIntegration.cs:27,85`.
- [ ] Locate other signatures rather than assuming token support.
- [ ] Preserve independent waiter cancellation and expected-cancellation feedback.

**Completion criteria:** Cancelled queued reads never send later. Disposal/supersession cannot publish late results or show expected-cancellation error toasts. Other valid waiters remain unaffected. Write behavior is unchanged.

_Requirements: 5.1–5.2, 5.4–5.5._

### Task 10. Add the interactive admission deadline

Update `Integration.NS/Services/NetSuiteApiClientService.cs:438–495` and the corresponding OAuth1 admission path after locating its current implementation.

- [ ] Add a configurable **10-second default admission-wait deadline for applicable interactive reads**.
- [ ] Return a typed busy outcome and distinguish caller cancellation, admission expiry, and HTTP timeout.
- [ ] Maintain correct permit ownership/release.
- [ ] Keep retries inside the existing permit hold; introduce no independent semaphore budget.
- [ ] Preserve existing write retry classification and uncertain-write handling.

**Completion criteria:** Saturated admission expires within its configured budget plus scheduling tolerance. Expiry before acquisition sends no request. Permits are neither leaked nor double-released. Existing resilience tests remain passing. Do not apply the admission default as a ten-second HTTP timeout.

_Requirements: 5.3–5.5, 6.2._

## Phase E — Measurement-Gated Query Work

### Task 11. Re-measure and decide whether SuiteQL changes are justified

- [ ] Repeat Task 1 measurements after core fixes.
- [ ] If remote execution dominates, capture query text/timings and evaluate scoped aggregate alternatives.
- [ ] Follow `.opencode/docs/SuiteQL-Shape-Rules.md`.
- [ ] Compare eligible IDs, counts, ordering, filters, and pages.
- [ ] Validate justified candidates in a representative sandbox before rollout.

Starting points:

- `Integration.NS/Helpers/SuiteQLFragments.cs:32–38,84–94`
- `Integration.NS/Implementations/Transactions/Packing/StockTransferRequestPackingIntegration.cs:52–54`

**Completion criteria:** Produce a justified equivalence-validated query improvement or explicitly defer query changes. No eligibility-after-pagination shortcut and no assumed-compatible correlation rewrite. Auto-fulfillment work is not an alternative task in this phase.

_Requirements: 6.3–6.5, 7.1; Acceptance Criteria: AC22, AC24._

## Phase F — Regression and Rollout

### Task 12. Complete regression verification and operational documentation

Run sequentially:

```powershell
dotnet test "Integration.NS.Tests/Integration.NS.Tests.csproj" --no-restore -v minimal
dotnet test "Web.BlazorServer.Tests/Web.BlazorServer.Tests.csproj" --no-restore -v minimal
dotnet build "Web.BlazorServer/Web.BlazorServer.csproj" --no-restore -v minimal
```

If dependencies are unavailable, report the restore limitation rather than treating verification as passed.

- [ ] Verify Packing STR, Returns, and VRA/RTS and Receiving PO, STR, and Returns.
- [ ] Verify Sync inside cache TTL, rapid filter/page/status changes, same/changed-scope failures, and print success followed by refresh failure.
- [ ] Verify saved settings/paging, selection, authorization/subsidiary isolation, and posting safeguards.
- [ ] Update `docs/packing-receiving-performance.md` with actual shipped behavior, deployment capacity assumptions, baseline/post-change measurements, limitations/deferred work, and rollback through the deployment's existing mechanism.
- [ ] Report results and remaining hosted-verification limitations honestly.

**Completion criteria:** Applicable suites/build pass; hosted evidence distinguishes queue, execution, and UI timing; production improvement is not inferred solely from test success; no auto-fulfillment changes are included.

_Requirements: 6.1–6.5, 8.1–8.4; Preservation Requirements 1–10._

## Explicitly Deferred

- Auto-fulfillment investigation, optimization, disabling, or removal.
- Distributed admission infrastructure.
- New Packing transaction-result TTLs.
- Write-pipeline redesign.
- Unmeasured SQL or IIS changes.
- Production load generation.

## Traceability Summary

| Requirement group | Tasks |
|---|---|
| 1. Reliable grid ordering | 2, 3, 4 |
| 2. Explicit refresh freshness | 2, 3, 6 |
| 3. Failure and staleness feedback | 2, 3, 5, 8 |
| 4. Request reduction and isolation | 2, 6, 7, 8 |
| 5. Cancellation and admission | 3, 9, 10 |
| 6. Capacity and measurement | 1, 10, 11, 12 |
| 7. Measurement-gated SuiteQL | 11 |
| 8. Regression validation | 2, 4, 5, 7, 12 |
| Preservation requirements | 12 and touched-path checks throughout |

Original Requirements 7.2–7.3 and AC23 are retired, not implementation obligations.
