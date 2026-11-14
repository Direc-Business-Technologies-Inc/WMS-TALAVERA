# Packing and receiving tab-loading improvements

## Purpose

Reduce avoidable NetSuite work and improve responsiveness when several users
access the Blazor Server application through IIS. The reported baseline was
approximately 10–12 seconds for packing, with receiving faster. Those figures
are user observations, not a benchmark collected during implementation.

## Implemented changes

| Area | Previous behavior | Current behavior |
| --- | --- | --- |
| Packing badges: STR, Returns, Returns to Supplier | Two fulfillment-list requests, returning unused rows to obtain totals | One aggregate request returns both totals |
| Receiving badges: PO, STR, Returns | Two receipt-list requests with display projection and sorting | One aggregate request returns both totals, without display projection or sorting |
| Receiving line-location eligibility | Join all matching source lines, then deduplicate headers/receipts | Join distinct eligible PO source IDs, retaining the any-authorized-line rule |
| Overlapping badge refreshes | Older requests continued independently | One active refresh and the latest pending refresh; superseded aggregate requests are cancelled |
| Tab/component disposal | Badge work could continue after its component was removed | Disposal cancels the active aggregate request and rejects stale results |
| Browser grid settings | Two sequential local-storage reads | Both reads are issued before either is awaited |
| Receiving diagnostics | Transport diagnostics available | List duration, requested page size and offset are logged as well |
| IF/IR list opening | Fetch every page before opening the selection dialog | Open the dialog immediately, with visible loading and retry feedback |
| IF/IR printing | Close the snapshot dialog after clicking Print | Keep it open, reload its current source/status scope, and notify the parent after a successful counter update |
| RTS printing | Parent refresh could reuse the supplier-return cache | Successful print updates invalidate the supplier-return entity as well |
| Badge refresh feedback | Previous totals cleared during refresh | Retain the last successful totals with an updating indicator |

For a cold tab with aligned paging, no retries and no extra lifecycle-triggered
loads, the primary request baseline changes from **one list + two badge queries**
to **one list + one badge query**. Receiving cache hits can reduce this further.
This is a request-count reduction, not a promised percentage latency improvement.

## Internal contract

The new MediatR operations are internal application queries; they do not add
public HTTP endpoints:

- `GetPackingPrintStatusCountsQry(DataGridIntent)`
- `GetReceivingPrintStatusCountsQry(DataGridIntent)`

Both return `PrintStatusCountsDTO`:

```json
{ "forprinting": 8, "printed": 3 }
```

The caller supplies source/category/date/status restrictions using the same
filter helpers as the corresponding receipt/fulfillment lookup, with
`PrintStatus.All`. These are **unpartitioned** filters: applying a Printed or
For Printing predicate before requesting both totals would filter out the other
bucket. Page size, offset and sorting do not affect aggregate totals.

The integration reuses the list query builder's joins, property mappings and
eligibility predicates, then replaces the SELECT with:

```sql
COUNT(DISTINCT CASE
  WHEN t.custbody_wms_print_count IS NULL
    OR t.custbody_wms_print_count <= 0 THEN t.id
END) AS ForPrinting,
COUNT(DISTINCT CASE
  WHEN t.custbody_wms_print_count >= 1 THEN t.id
END) AS Printed
```

The response is one aggregate row, even when no transactions qualify; its values
are read from the row, not NetSuite's `totalResults` (which counts result rows).
A missing aggregate row is treated as an error, rather than silently displayed
as zero. Badge failures retain the existing unavailable (`—`) presentation.

### Business rules preserved

- Badges count **item receipts or item fulfillments**, not source documents.
- A source document can have both printed and unprinted child transactions.
- Sources with no child receipts/fulfillments retain their existing list-view
  eligibility; this is separate from child-transaction badge totals.
- Packing keeps label eligibility, source subsidiary, source type, category and
  source-state restrictions.
- Receiving keeps purchase-category/approval rules, source-state restrictions,
  subsidiary and location authorization, source-date ranges and selected status.
- PO authorization still means **at least one source line in an allowed
  location**. It does not become a mainline/header-location check.
- Receiving's authorized-line derived table is filtered to purchase orders and
  allowed locations before reducing it to one source ID. It is an inner join for
  the PO list and a left join for receipt queries, so STR/Returns eligibility
  remains independent of the PO branch.

### Cancellation and caching

`PrintStatusView.CountsGetter` accepts a `CancellationToken`. The token flows
through the web handler, MediatR, integration and SuiteQL HTTP client. A refresh
during active work cancels that request and queues only the newest refresh.
Version checks additionally prevent stale results from replacing newer values.
Disposal cancels active work. Cancellation of an HTTP request does not guarantee
NetSuite stops a query already executing remotely.

Legacy `CountGetter` consumers remain supported. Their two calls execute
in parallel as before, and stale results are ignored; their existing delegate contract
does not support transport cancellation.

Receiving aggregates use the existing per-user, intent-keyed receiving cache,
TTL, duplicate-miss suppression and receiving invalidation boundary. They do not
introduce a separate cache lifetime. Successful receipt posting invalidates the
receiving entity as before. External changes may remain cached until expiry.
Cache keys also include an entity generation: a read that began before a print
or other mutation cannot repopulate the currently visible generation after
invalidation. This applies to receiving and RTS, including overlapping reads.

Foreground grid reads still use the shared grid's latest-pending-load behavior;
end-to-end cancellation added here is specifically for aggregate badge reads.

## Print-button workflow and colleague feedback

The print-button click is the event being counted. The browser opens the native
new-tab link immediately; the application does not wait for the operator to
finish or dismiss a browser print dialog. Repeated activation is blocked while
that control's update/refresh is pending. Null/zero/negative counters become 1
on the first successful print-count write; positive counters increment normally.

After a successful update:

- IF/IR selection dialogs reload their own data and notify their parent while
  remaining open. Packing STR, Returns and VRA/RTS labels share one row-print
  component, so these workflows do not have separate refresh implementations.
- The parent table and badge refreshes start together where their scopes are
  independent, rather than deliberately waiting for table completion first.
- Standalone Returns to Supplier printing also refreshes rows and badges, with
  supplier-return cache invalidation following a successful write.
- In For Printing, the printed child disappears from the refreshed modal; in
  Printed/All, it remains available for reprinting. The modal shows an empty
  state when no children match. Receiving now shows the child print status too.

Initial list loading and modal reloads show an indeterminate loading indicator.
Load failures show inline retry feedback. Closing a modal during a read prevents
its late response from updating that disposed modal. Existing list-read APIs do
not support cancellation, so an already-running list request may still finish.

### Trip Ticket error diagnosis

`AppPrintLink` now separates counter-update failures from list-refresh failures.
An unsuccessful NetSuite write logs the record type/ID and exception and does
not report success or invoke the parent refresh. A successful write followed by
a refresh error instead shows **Refresh needed**, explicitly saying that the
counter was updated and that reprinting is unnecessary to refresh the list.

Trip Ticket additionally logs whether the failure occurred while reading the
custom-record counter or PATCHing it. Its existing custom record and field IDs
remain unchanged. Transport tests cover successful empty HTTP 204 responses and
rejected permission writes. The screenshot did not include the server exception,
so an account-specific field permission, REST exposure, or script rejection still
requires its actual NetSuite error; these errors are surfaced, not suppressed.

### Dates remain operator-controlled

There is intentionally **no default date restriction** on initial load. This is
separate from the date-picker controls, which are restored on all three Packing
tabs: Stock Transfer Request, Returns, and Returns to Supplier.

- **Source-order date** filters the source document's `t.trandate`, not the item
  fulfillment date or last-modified timestamp.
- On each successful load before the operator applies a range, the picker shows
  the earliest/latest non-null source-order dates on the **visible page**. Paging,
  status changes and Sync update these display values from the loaded rows. This
  is not an all-pages range and is not an automatic filter. Empty pages or pages
  with only null dates show empty pickers; dates are never fabricated as today.
- Automatic display values use the already-loaded records and cause no extra
  backend query. Separate applied-range state prevents those dates from silently
  restricting the next page or Sync request.
- The picker contains only the two date inputs and **Apply**, matching the other
  transaction pages. Today/Clear buttons, the source-date label, and the helper
  paragraph have been removed at the operators' request.
- Edit the dates and select **Apply** to filter. The start date is inclusive;
  the end date includes the entire day (`Date >= start.Date AND Date < end.Date +
  1 day`). Missing or reversed bounds disable Apply. All three Packing queries
  explicitly map `Date` to native `t.trandate`, retaining formatted SELECT values
  only for response serialization.
- Apply resets pagination to page one with one fresh row read. Once applied, the
  selected range remains visible across paging, status changes, Sync, printing,
  and even empty results. It is no longer overwritten by page-derived dates.
  Unrelated parent renders do not discard draft picker edits. Reopening the tab
  in a new component instance starts without an applied date range.
- Packing badges remain **global eligible item-fulfillment counts** for the
  source/subsidiary scope. They do not follow the source-order date picker or
  arbitrary grid column filters. Grid pagination totals count **source orders**;
  these totals may differ. Applying dates does not requery badges.

Displaying or editing dates adds no NetSuite request to initial loading. The
cost of an applied date-filtered query depends on actual backend execution; no
latency improvement is claimed without measurement.

### Date display extended to the other transaction pickers

The same page-derived display behavior now covers every existing transaction
`GridDateRange` consumer, in addition to Packing:

| Transaction | Date represented by the picker |
| --- | --- |
| Receiving → Purchase Orders | Source purchase-order date (`Date`), not delivery/receipt date |
| Receiving → Stock Transfer Request | Source transfer-order date (`Date`) |
| Receiving → Returns | Source return date (`Date`) |
| Inventory → Stock Transfer Request → Transfer Orders, Intercompany Transfer Orders, Returns | Transaction date (`Date`); the tabs share `STRDataGrid` |
| Returns to Supplier | Supplier-return transaction date (`Date`) |
| Trip Tickets | Trip date (`TripDate`), not source-order date |

Before Apply, the two picker values reflect the earliest/latest available dates
on the current successfully loaded page. Empty pages and pages with no non-null
dates show empty pickers. Loading another page or refreshing may change those
display values; no all-pages date query is performed.

Display values are explicitly separated from applied filtering. Until Apply is
selected, they add no restrictions to row reads **or badge counts**. After Apply,
the selected inclusive range is preserved across paging, status changes, refreshes,
and empty results. Existing badge scope rules remain: Receiving counts filter
source dates through `tcf.trandate`, Inventory STR/supplier-return counts follow
their transaction-date scope, and Trip Ticket counts follow the captured trip-date
scope. The UI remains the same two date inputs and Apply, without added labels or
Today/Clear buttons. A new component instance starts without an applied range.

### Inventory STR count-recovery fixes (2026-11-09)

The reported count-failure screenshot is from Inventory → Stock Transfer Request,
which uses the legacy two-count path rather than Packing's aggregate path.

- Inventory STR count reads now explicitly request `GridReadPolicy.Fresh`, so
  **Retry counts** cannot clear the stale marker merely by reusing completed
  cached totals. The two reads retain their existing status/date restrictions.
- Sync uses the shared grid's fresh-read API and refreshes badges concurrently.
  Its normal failure feedback remains inline.
- Failed row loads propagate the unsuccessful action to the shared grid. A
  same-scope failure retains previously successful rows with stale feedback;
  it is no longer converted into a successful empty result.
- Feature filters are prepared before the shared grid captures the request scope.
  A failed change of print status or dates therefore clears previous-scope rows
  instead of retaining them under the newly selected filter.
- Post-print callbacks propagate refresh failures so the print control can show
  **Refresh needed** after a successful counter update.

These fixes improve recovery semantics; they do not diagnose the original remote
failure. Correlate the count timestamp, grid/status timing logs, and NetSuite
transport exception/status to identify timeouts, throttling, or query failures.

## Relevant implementation files

- `Application.DataTransferObjects/Transactions/PrintStatusCountsDTO.cs`
- `Application.UseCases/Queries/Transaction/Packing/GetPackingPrintStatusCountsQry.cs`
- `Application.UseCases/Queries/Transaction/Receiving/GetReceivingPrintStatusCountsQry.cs`
- `Integration.NS/Services/SuiteQLQueryBuilder.cs` — aggregate projection
- `Integration.NS/Implementations/Transactions/Packing/ReturnPackingIntegration.cs`
- `Integration.NS/Implementations/Transactions/ReceivingIntegration.cs`
- `Integration.NS/Implementations/Others/Caching/CachedReceivingIntegration.cs`
- `Web.BlazorServer/Components/Custom/PrintStatusView.razor`
- `Web.BlazorServer/Services/Implementation/GridSettingsService.cs`

## Automated verification

```powershell
dotnet build "Web.BlazorServer/Web.BlazorServer.csproj" --no-restore -v minimal
dotnet test "Integration.NS.Tests/Integration.NS.Tests.csproj" --no-restore -v minimal
dotnet test "Web.BlazorServer.Tests/Web.BlazorServer.Tests.csproj" -v minimal
```

`PrintStatusCountsTests` covers one aggregate request, unchanged list/count
eligibility, distinct transaction counting in the SQL shape, no display/sort
projection, source/date/status filters, location gate shape, cancellation-token
propagation, lowercase response aliases, and receiving cache/invalidation.
Existing packing eligibility and grid cache tests provide regression coverage.
These tests use a capturing NetSuite client; they do not execute SuiteQL against
a live account or prove the remote optimizer's execution cost.

`Web.BlazorServer.Tests` adds bUnit coverage for native-link activation, duplicate
click suppression, failed writes versus failed refreshes, RR/Packing Label
loading/retry and open-modal refresh, STR/Returns/VRA sources, RTS callbacks,
operator-controlled dates, and badge refresh/coalescing/disposal. Transport tests
exercise the real print-count client against a local HTTP stub, including RTS
cache invalidation and successful Trip Ticket PATCH responses with no body.

### Results from implementation verification

#### Transaction-wide page-derived date display — 2026-11-09

- Added six parameterized component scenarios covering Receiving PO/STR/Returns,
  Inventory STR, standalone Returns to Supplier, and Trip Tickets.
- Coverage verifies display-only initial min/max values, unrestricted initial
  row/badge requests, date updates after paging, empty-page handling, no fetch
  while editing, Apply restrictions on both rows and badges, first-page reset,
  and retention of an applied range after paging and empty refresh results.
- Focused `PrintWorkflowTests` and `ApprovedPrintStatusFilterTests`: **66 passed,
  0 failed**. The web project incremental build passed with zero warnings/errors;
  `git diff --check` passed. No live NetSuite or deployed-browser check was run.

#### Page-derived picker dates and review corrections — 2026-11-09

- Focused `PrintWorkflowTests` and `ApprovedPrintStatusFilterTests`: **60 passed,
  0 failed**. Updated coverage verifies page-derived min/max dates without date
  filters or extra initial reads; paging; empty/null-date pages; Apply-only UI;
  explicit inclusive ranges retained after status/Sync/empty results; and
  Inventory STR clearing old rows on failed changes of print status/date scope.
- Focused `SuiteQLDateFilterTests` and `PrintStatusCountsTests`: **16 passed,
  0 failed**. Captured SQL verifies native `t.trandate` comparisons for STR,
  Returns, and Returns to Supplier through their real integration list builders.
- Both focused test commands compiled their projects successfully.
- `git diff --check`: passed. No live NetSuite or deployed-browser check was run.

#### Initial picker restoration (superseded UI) and Inventory STR recovery — 2026-11-09

- `dotnet build "Web.BlazorServer/Web.BlazorServer.csproj" --no-restore -v quiet`:
  passed, zero warnings and errors in this incremental build.
- Focused `PrintWorkflowTests` and `ApprovedPrintStatusFilterTests`: **60 passed,
  0 failed**. Coverage includes all three Packing sources with/without saved
  paging; Today without a fetch; Apply/Clear; full-day bounds; date retention
  across status/Sync; draft retention; and Inventory STR failure/retry freshness.
- Root `dotnet build --no-restore -v quiet` exceeded the 240-second timeout
  while emitting MAUI warnings. The web build and focused test build succeeded.
- `git diff --check`: passed. No live NetSuite or deployed-browser check was run.

#### Earlier aggregate/print-workflow implementation

- Web project build: passed, zero errors; existing compiler/analyzer warnings
  remain.
- Web print-workflow tests: **17 passed, 0 failed**.
- Full integration suite: **802 passed, 0 failed**.
- Root `dotnet build` exceeded the 240-second command timeout. The web project
  and both test projects compiled successfully in their focused test runs.
- No live NetSuite execution, browser smoke run, IIS deployment or colleague-side
  benchmark was performed during this verification.

## IIS verification and comparison procedure

Use the same hosted build, permissions, subsidiary, source tab, filters and page
size for the developer and a colleague. Compare each browser with its saved
settings and with a clean browser profile. A local development instance and a
remote IIS instance are not an equivalent comparison.

Record first visit after idle/recycle, warm revisit, tab switching and several
simultaneous users separately. Record at least:

| Measurement | Source |
| --- | --- |
| Click → first rows visible | Browser Performance recording |
| Click → both badges visible | Browser Performance recording |
| Settings initialization | Browser/SignalR trace |
| Foreground list duration | Packing logs; new `Receiving {GridName} completed...` log |
| NetSuite permit wait | Existing NetSuite client permit-wait logs |
| HTTP duration, attempts and retries | Existing NetSuite client transport logs |
| Connection transport/reconnects | Browser Network trace for `/_blazor` |

Interpret the phases independently:

- High permit wait: correlate with concurrent work and retries before changing
  the global concurrency allowance.
- High HTTP duration: investigate the SuiteQL/query/data-source phase.
- Fast server fetch but late visible rows: examine browser rendering, row count,
  saved settings and the live Blazor connection.
- Slow only after idle/recycle: inspect effective IIS app-pool startup/idle and
  recycle settings alongside warm timings.

### Functional smoke checks

1. Open all six tabs; rows should appear before optional badges finish.
2. Compare badge totals with receipt/fulfillment selections for each source.
3. Check no eligible children, null/zero/negative print count, printed children,
   and a source with mixed printed/unprinted children.
4. For receiving, change source-date ranges and selected status; both totals
   must follow those filters.
5. Check a PO with multiple lines in allowed locations, a PO with only one
   authorized line, and a PO with no authorized lines. No duplicates or broader
   visibility should appear.
6. Switch tabs and filters rapidly during a slow badge request. Old totals must
   not replace the latest totals; cancelled requests should not be logged as
   badge failures.
7. Navigate away during badge loading, then return. Verify counts can load again
   without component-disposal errors.
8. Exercise paging, saved filters, stale saved offsets and receipt-posting cache
   invalidation.
9. Open RR/IF selectors for multi-child documents: loading feedback must appear
   before list completion. Print one child; keep the modal open and confirm that
   it disappears in For Printing or updates in All/Printed, while parent badges
   and rows reconcile automatically.
10. Print a Returns to Supplier document and a packing VRA/RTS label; verify
    automatic refresh for both, without waiting for the cache TTL.
11. In Trip Ticket, verify a successful print click, a rejected counter write and
    a failed list refresh independently. Only rejected writes should display a
    print-count error; refresh failures after successful writes must display
    the distinct refresh warning.

## Measurement-dependent follow-ups

The review also identified broad historical aggregate inputs in packing and
receiving, possible tab recreation, and possible additional reload callbacks.
Those are optimizer/lifecycle hypotheses until measured. This change does not
rewrite historical aggregate scope, switch Radzen tab rendering, increase
NetSuite concurrency, or change IIS configuration. Evaluate those only against
captured timings and identical eligibility results. NetSuite has known
limitations with correlated subqueries, so a conventional SQL EXISTS rewrite
must not be assumed compatible.

## 2026-11-06 — Grid refresh reliability

Implemented against source baseline `2671287d` on
`claude/fix/web-grid-refresh-reliability`. This is a local, uncommitted build;
it is not a deployed web/API build identifier.

### Implemented behavior

- Shared Radzen callbacks, direct reloads and QuickDataGrid reloads now use one
  coordinator: one active request and the newest pending request. Requests have
  independent intent/filter snapshots, generation-checked publication and
  explicit succeeded/failed/cancelled/superseded/busy outcomes. Disposal completes
  waiters and rejects late results.
- Sync requests fresh data. Receiving bypasses completed entries without evicting
  other users' cached pages; successful fresh reads populate the normal cache.
  Packing retains its current page on Sync; Receiving's first-page refresh and
  QuickDataGrid's first-page operation still reset the pager. Ordinary Radzen
  loads retain normal cache policy.
- Same-scope failure retains successful rows with a stale indicator and Retry
  Sync. Changed-scope requests hide previous-scope rows immediately. Empty success
  clears the error; cancellation and busy admission are distinct outcomes.
- Packing STR, Returns and VRA status selection reload rows without recomputing
  global totals. Initial loading, explicit Sync and post-print reconciliation
  still refresh totals. Failed badge reads retain totals with a stale marker and
  retry action. Post-print row/dialog/badge failure reaches the existing
  "Refresh needed" warning without repeating the counter write. Ordinary Sync
  displays errors in the grid rather than throwing from the UI event.
- The existing integration cache now shares overlapping success **and failure**.
  Completed flights are removed. Keys include identity, authorization claims,
  subsidiary, full business intent, normal/fresh policy and mutation generation.
  Each waiter can cancel independently; the last waiter leaving cancels upstream
  work. Packing list/count sharing stores no completed transaction-result TTL.
  Successful writes advance Packing's read generation; existing Receiving print
  and receipt mutation invalidation remains in place.
- Cancellation travels with internal read metadata through feature handlers,
  MediatR, integrations, paging and transport. Policy and cancellation are JSON
  ignored; neither is a fake business filter or a public endpoint parameter.

### Admission configuration and diagnostics

`NETSUITE_INTERACTIVE_ADMISSION_SECONDS` is a positive finite number of seconds,
default **10**. It bounds only permit admission for interactive reads, including
OAuth1. Expiry throws the typed busy outcome before HTTP; caller cancellation
stays cancellation. HTTP timeout remains separately controlled by
`NETSUITE_HTTP_TIMEOUT_SECONDS` (default 100 seconds). Writes retain their retry
classification and permit hold across backoff.

Admission logs include acquired/busy/cancelled, elapsed wait, request kind,
profile, effective process budget and deadline. SuiteQL logs retain trace-linked
HTTP attempt durations and attempt count, and add total HTTP time and the
retry/scheduling gap. That gap includes pipeline/scheduler overhead; it is not a
remote-execution measurement. Continue measuring click-to-visible-rows in the
browser; a server-side completion timestamp does not measure browser paint.

### Capacity inventory and hosted evidence still required

| Item | Evidence available in this session |
| --- | --- |
| Local source / SDK | `2671287d` plus working tree / .NET SDK `9.0.317` |
| Deployed web build | Unavailable; deployment operator must record |
| Deployed API build | Unavailable; deployment operator must record |
| Effective worker/process counts and limiter overrides | Unavailable |
| IIS web garden, recycle/deployment overlap | Unavailable |
| Other NetSuite account consumers | Unknown, not assumed absent |
| Account limit | Five, as specified |
| Default local budget | Two per process; not an account-wide budget |
| Cold/warm/concurrent before-and-after timings | Not captured |

Planning arithmetic: **one** web process at two permits plus **one** API process
at two permits uses four of five slots, leaving one slot only if other traffic
fits that headroom. This is an assumption, not a verified deployment inventory.
Two overlapping copies of both processes would budget eight permits and exceed
five. Operators must account for all workers, deployment overlap and external
consumers before rollout; the patch does not change IIS or raise any limit.

For each pre/post run, record deployed build IDs, permissions, subsidiary, source,
filters, page size and dataset, then capture the timing phases in the IIS
procedure above for idle/recycle, warm visits, Sync inside TTL, rapid filter/page
changes and representative concurrent usage. No hosted benchmark or production
load generation was performed. Production latency improvement remains unverified.

### Verification and follow-up

Deterministic barrier tests cover shared-grid overlap and completion, snapshot
isolation, stale/changed-scope/empty outcomes, disposal, paging and selection;
all three Packing status selectors; fresh cache reads, shared success/failure,
independent cancellation, mutation generations and user/subsidiary/authorization
isolation. Local HTTP-stub tests cover OAuth1/OAuth2 admission expiry, queued
cancellation and permit ownership alongside existing write-safety tests.

The original tests were added during implementation; they were not replayed against a
separate checkout of the unfixed baseline. Defect characterization before edits
was source-based, so no failing-baseline test run is claimed.

SuiteQL changes are explicitly deferred: no candidate query was equivalence- or
sandbox-validated. The follow-up trace below shows expensive HTTP operations and
redundant reads, but does not isolate pure database execution from network time.
Auto-fulfillment, posting validation and payloads are outside this patch.

Rollback uses the deployment's existing previous-artifact redeployment mechanism
for the affected hosts. No migration or data rollback is required. Restore the
previous environment settings with that deployment; removing only the admission
override restores the ten-second default, not the previous implementation.

See the spec's [implementation record](../.opencode/specs/2026-11-06-web-grid-refresh-reliability/implementation.md)
for automated verification results and remaining rollout work. Graphify was not
updated, and no commit or push was performed for this implementation.

### Runtime-regression review — all Packing and Receiving tabs

The user's STR trace showed three first-attempt records queries for the same
page, taking approximately **6.16 s, 4.25 s and 4.35 s** in HTTP time. Their
permit waits were only a few milliseconds, with no retries. The badge query
overlapped the last records query. Duplicate initial reads are therefore an
actionable contributor to this trace's roughly fifteen-second records workload.

Review found that enabling the getter during settings setup, explicitly reloading
settings/data, then changing visibility could each cause a read. The awaited
coordinator did not deduplicate those sequential operations. The earlier tests
missed the complete page/settings lifecycle.

The correction applies to **Packing STR, Returns and RTS/VRA**, and **Receiving
PO, STR and Returns**:

- Defer application reads until saved settings and columns can be applied after
  rendering; use that one settings application for the initial records request.
- Remove the pages' additional initialization reloads. Start Packing badges once
  after successful rows publication.
- Bind settings back to their owning pages so badge rerenders cannot restore an
  old page after Sync.
- Restore Receiving STR/Returns reference filters as strings, matching their
  ViewModels rather than the former integer column declaration.

Nine added lifecycle cases verify one initial records request per tab, saved
paging/sorting/alphanumeric filters, Packing defaults without saved settings,
rows-before-badges ordering and one fresh row/badge reconciliation on Sync.
Verification: **815 integration tests and 38 web tests passed; web build passed**.
The web tests/build used isolated temporary output because the running debug app
locked normal binaries. Stop, rebuild and restart that app before retesting.

For the next capture, expect one initial `Records` operation and one `Counts`
operation per newly opened tab without user actions. Record visible-rows time
separately from badge completion. A deliberately invalid saved offset may still
require the existing offset-clamp recovery. Individual NetSuite query durations
and end-to-end improvement remain subject to that new runtime measurement.
