# Implementation record — Web grid refresh reliability

- Date: 2026-11-06
- Source baseline: `2671287d`
- Branch: `claude/fix/web-grid-refresh-reliability`
- Status: code implemented; hosted deployment/performance verification pending
- Requested constraints: no commit, no push, no Graphify update

## Implementation decisions

The existing `DataGridIntent` carries JSON-ignored `GridReadPolicy` and
`CancellationToken` execution metadata. Its `Snapshot()` copies filter trees,
sorts and paging without serializing object-valued filters into `JsonElement`s.
The existing feature-handler → MediatR → integration path carries that metadata;
there is no new endpoint, fake filter or random cache key.

`AppDataGrid` coordinates all load paths, snapshots feature filters at enqueue
time via `PrepareIntent`, retains the latest pending request, explicitly completes
superseded/cancelled callers and guards publication. It retains same-scope success
with error feedback, hides changed-scope rows, and exposes outcome/exception
metadata. Fresh first-page reloads preserve Radzen pager behavior; direct Sync
retains saved paging. QuickDataGrid uses the same coordinator. Touched feature
loaders rethrow captured failures/cancellation before constructing a successful
grid result.

`GridResultCache` now shares actual tasks instead of serializing cache misses.
Waiters independently cancel, completed flights are removed, and success/failure
is shared only for compatible scope/policy/generation. A newer fresh result cannot
be overwritten in normal cache by an older normal completion. The existing
`GridCacheStore` owns process-wide flights and completed-cache storage. Packing
list/count integrations use it only for in-flight sharing, with no result TTL.
Production DI supplies the existing scoped cache; optional constructor defaults
preserve existing direct fake-client test construction.

Packing status changes issue no badge read. Badge failures retain totals with a
stale/retry marker. Sync uses non-throwing outcome feedback; post-print callbacks
propagate unsuccessful reconciliation to the existing print-success/refresh-needed
warning. Dialog reconciliation failures also propagate after their local error
state is set.

The OAuth1 and OAuth2 admission paths share the existing process semaphore and
apply the configurable ten-second default to interactive reads only. Expiry is
`GridAdmissionBusyException`, independent of HTTP timeout and caller cancellation.
Retries stay inside the existing permit hold. Admission and SuiteQL logs separate
queue, HTTP attempts, retry/scheduling gap and total duration. Browser paint timing
still requires a browser capture.

## Task disposition

| Task | Disposition |
| --- | --- |
| 1 — Deployed baseline/inventory | Pending hosted access. Local source/SDK and explicit unknowns are documented in the performance guide. |
| 2 — Characterization | Deterministic regression cases added. Source review demonstrated defects; no separate failing-baseline checkout run was performed. |
| 3 — Contracts | Implemented read policy, refresh outcomes, independent snapshots and failure metadata. |
| 4 — Coordinator | Implemented shared ordering/completion for Radzen, both direct overloads, first-page refresh and QuickDataGrid. |
| 5 — Honest read outcomes | Implemented for the six affected grids and QuickDataGrid, with post-print warning propagation. |
| 6 — Fresh reads | Implemented Receiving fresh bypass/population without unrelated eviction. |
| 7 — In-flight sharing | Implemented task sharing, cancellation isolation, scope/policy/generation isolation and flight cleanup; no Packing TTL. |
| 8 — Badges | Implemented all three Packing selectors and stale/retry feedback. |
| 9 — Cancellation | Implemented touched list/count paths through existing intent metadata and paging/transport tokens. |
| 10 — Admission | Implemented both authentication paths with deadline, typed busy and permit-ownership tests. |
| 11 — SuiteQL | Explicitly deferred for lack of hosted execution measurements; query shapes unchanged. |
| 12 — Regression/rollout | Automated suites/build verified below. Hosted smoke checks, real build IDs, inventory and timings remain pending. |

The original task checklist remains a planning/rollout checklist. This record
distinguishes code delivery from operational acceptance; unobserved deployment
facts are not marked complete.

## Automated verification

Sequential commands from the repository root:

```powershell
dotnet test "Integration.NS.Tests/Integration.NS.Tests.csproj" --no-restore -v minimal
dotnet test "Web.BlazorServer.Tests/Web.BlazorServer.Tests.csproj" --no-restore -v minimal
dotnet build "Web.BlazorServer/Web.BlazorServer.csproj" --no-restore -v minimal
```

- Integration: **815 passed**, zero failed/skipped.
- Web: **38 passed**, zero failed/skipped, including nine lifecycle cases added
  during the runtime-regression review below. The final web run also supplied
  `-p:WarningLevel=0` to suppress existing compiler warnings.
- Web build: succeeded, zero errors. Follow-up web verification used an isolated
  output directory because the running app/debugger locked the normal output.
- Existing compiler/analyzer warnings were present in test compilation.
- No live NetSuite requests, production load test, IIS deployment or hosted
  browser performance capture was performed.

Added coverage uses task-completion barriers for overlap rather than sleeps.
The actual Radzen first-page test exposed a duplicate normal callback following
the fresh callback. Identical callbacks now share only their still-current
navigation's outcome; a direct Sync always enters coordination independently.
The pager retains same-filter totals during page changes to avoid a loading-time
reset, and live subsidiary changes prevent obsolete publication.
Direct reload regression coverage also verifies that updated default filters
replace previous defaults without accumulating, and the filter-removal overload
can explicitly exclude them.
Admission expiry uses a short configured deadline against occupied real permits;
transport uses existing local HTTP stubs. Existing integration tests retain
eligibility, query-shape, receipt/fulfillment payload and safe-write coverage.

## Follow-up review — repeated initial reads across tabs

The user supplied a slower STR startup trace after the initial implementation.
It contains three successful STR records operations for `skip=0`, `take=10`:

| STR operation | SuiteQL HTTP time | Permit-wait log | Attempts |
| --- | ---: | ---: | ---: |
| First | 6,160 ms | 3.2 ms | 1 |
| Second | 4,248 ms | 4.4 ms | 1 |
| Third | 4,347 ms | 3.4 ms | 1 |

Those reads represent about 14.8 seconds of HTTP work. The counts query took
about 1,957 ms and overlapped the last records query. This trace shows redundant
records requests, not semaphore saturation or retry backoff. The unlabeled
bootstrap requests and browser paint were not independently correlated.

### Findings and corrections

1. **Repeated initialization:** The pages enabled loading before settings setup
   finished, called `ReloadSettings()` and `Reload()` explicitly, then exposed the
   grid. Radzen's visibility/settings callbacks added further load opportunities.
   The awaited coordinator did not eliminate these sequential duplicates; in-flight
   sharing cannot merge reads that start after earlier ones finish. The initial
   web tests did not exercise the full saved-settings lifecycle.
2. **Premature settings reads:** Radzen 9.0.8 invokes its visibility reload before
   applying settings in `OnAfterRenderAsync`. Simply removing explicit reloads
   caused default paging to load instead of the saved scope. All six grids now
   opt into `DeferInitialLoad`. Early callbacks perform no application read;
   the shared grid applies settings once after rendering and lets that operation
   issue the initial records request with initialized columns and paging.
3. **Stale parent settings:** Settings updates were retained only inside the
   shared wrapper. A badge-triggered parent render could reapply old settings
   after Sync. Added `GridSettingsChanged` and two-way settings binding on all six
   pages; premature initialization callbacks also cannot overwrite saved state.
4. **Reference filter type:** Receiving STR/Returns declared string reference
   numbers as `int`, which broke restoration of alphanumeric saved filters.
   Their column types now match their string ViewModel properties.

Scope includes Packing STR, Returns and RTS/VRA, plus Receiving PO, STR and Returns.
Packing badges now start once after the first successful records publication,
using `OnDataLoaded` rather than a page `OnAfterRenderAsync` flag.

Nine additional lifecycle cases use the real settings service, delayed browser
storage, actual Radzen components and controlled records completion. They cover
all six tabs, Packing with/without saved settings, saved paging/sorting/text
filters, records-before-badges ordering and one fresh row/badge reconciliation on
Sync. Final fixtures return populated pages so empty-page recovery is not counted
as a duplicate initialization read. The new cases failed against the pre-follow-up
working tree before the lifecycle/settings corrections; this is separate from
the original source-based baseline characterization.

The full integration run also exposed a pre-existing order-dependent test leak:
the limiter load fixture left the static HTTP client configured with a 30-second
test timeout. Its `finally` now restores the client. Production timeout settings
were not changed by this follow-up.

### Follow-up verification commands

```powershell
dotnet test "Integration.NS.Tests/Integration.NS.Tests.csproj" --no-restore -v minimal -p:WarningLevel=0
dotnet test "Web.BlazorServer.Tests/Web.BlazorServer.Tests.csproj" --no-restore -v minimal -p:WarningLevel=0 -p:BaseOutputPath="C:\Users\LUMAPA~1\AppData\Local\Temp\opencode\wms-grid-review-bin\" "-p:DefaultItemExcludesInProjectFolder=**/bin/**"
dotnet build "Web.BlazorServer/Web.BlazorServer.csproj" --no-restore -v minimal -p:WarningLevel=0 -p:BaseOutputPath="C:\Users\LUMAPA~1\AppData\Local\Temp\opencode\wms-grid-review-bin\" "-p:DefaultItemExcludesInProjectFolder=**/bin/**"
```

Results: **815 integration tests passed; 38 web tests passed; web build passed**.
The isolated output and additional bin exclusion are command-line verification
settings, not repository build-configuration changes. An initial isolated-output
integration run could not locate two source-tree SQL fixtures; the complete suite
was rerun successfully using its normal repository-local output.

Stop/rebuild/restart the debug app before capturing new timings; its currently
loaded binaries were not replaced by the isolated build. Compare records-query
count per tab first. Actual post-fix NetSuite/browser latency still needs a new
runtime capture; no fixed seconds target is inferred from these tests.

## Remaining operational acceptance

- [ ] Record deployed web/API artifact IDs and all NetSuite-calling processes.
- [ ] Verify per-process settings, IIS workers, overlap and other account traffic
      against the fixed five-slot limit and explicit headroom.
- [ ] Capture equivalent pre/post idle/recycle, warm, Sync, rapid-action and
      representative concurrent measurements, including browser paint timing.
- [ ] Smoke-test all six Packing/Receiving tabs, saved settings and selection on
      the hosted deployment, including successful print followed by read failure.
- [ ] Attribute remote execution cost before proposing any SuiteQL rewrite.

Use the capacity inventory, measurement procedure and rollback instructions in
[`docs/packing-receiving-performance.md`](../../../docs/packing-receiving-performance.md).
Passing tests establish local correctness; production latency is unverified.
