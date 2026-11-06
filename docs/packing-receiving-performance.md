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
not a missing feature to restore. The existing transaction-eligibility rules
still apply; date filtering is added only when the operator selects and applies
a range. No date initialization was added by these changes.

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

No graphify rebuild, branch creation or commit is part of this change.
