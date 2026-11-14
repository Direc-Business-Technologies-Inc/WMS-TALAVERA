# Requirements — Web Grid Refresh Reliability and NetSuite Capacity Optimization

- **Date:** 2026-11-06
- **Status:** Confirmed
- **Mode:** BUGFIX
- **Baseline reviewed:** `2671287d`
- **Scope:** Packing and Receiving grids, shared grid-loading behavior, read caching/cancellation, and NetSuite capacity diagnostics.
- **Related:**
  - [Interim NetSuite optimization](../2026-10-07-netsuite-interim-optimization/requirements.md)
  - [Web data-fetch optimization](../2026-10-08-web-fetch-optimization/requirements.md)
  - [Packing/receiving performance](../../../docs/packing-receiving-performance.md)
  - [SuiteQL shape rules](../../docs/SuiteQL-Shape-Rules.md)
  - [Grid paging stability](../../docs/grid-paging-state-stability.md)
- **Companions:** [Design](design.md), [Tasks](tasks.md)

## Confirmation and Scope Amendment

The user confirmed requirements, design, and tasks in sequence, then explicitly authorized writing this package. This specification authorizes planning documentation, not implementation.

Auto-fulfillment investigation and optimization are excluded. The user stated that the feature is no longer needed and may be removed if later evidence implicates it. That conditional permission is recorded; this package neither assumes it caused the slowness nor schedules its disabling/removal.

Original Requirements 7.2–7.3 and AC23 concerning auto-fulfillment optimization were removed before design confirmation. AC23 remains retired so the other confirmed acceptance-criterion IDs stay stable. AC24 applies only to measurement-gated SuiteQL changes.

## Repository Context

- Web and NetSuite integration target .NET 8; the API targets .NET 9.
- Components use existing feature-handler interfaces and MediatR dispatch boundaries.
- Existing `Integration.NS.Tests` and `Web.BlazorServer.Tests` provide the validation foundation.
- NetSuite admission is process-local, defaulting to two operations per process. Distributed admission was deliberately deferred.
- This work must not expand into unrelated architectural-debt cleanup, authentication changes, or SAP changes.

## Introduction

Operators report slow Packing grid loading, inconsistent refresh behavior, and frequent NetSuite `429 CONCURRENCY_LIMIT_EXCEEDED` errors. The account concurrency limit is fixed at five.

Review identified explicit Receiving refreshes reusing cached results, inconsistent shared-grid ordering/completion, failed reads becoming empty results, avoidable Packing badge reads, incomplete read cancellation, and deployment assumptions behind process-local admission.

The contribution of queueing, retries, SuiteQL execution, repeated callbacks, and browser rendering to the reported 10–15 seconds remains unverified. Correctness and request reduction come first; query optimization requires measurements.

## Glossary

- **Account concurrency limit:** The fixed maximum of five simultaneous requests governed by the relevant NetSuite account limit.
- **Process-local limiter:** The semaphore shared by NetSuite business requests within one running application process.
- **Grid scope:** Authorization context, subsidiary, source/category, filters, sorting, pagination, and other inputs defining a grid result.
- **Explicit refresh:** An operator's Sync/Refresh action requesting current data rather than ordinary cache reuse.
- **Reconciliation:** Updating affected rows, dialogs, and badge totals after a successful mutation.
- **Superseded read:** A read whose result is no longer applicable because a newer request or scope has replaced it.
- **In-flight coalescing:** Sharing one overlapping read among callers requesting the same authorized scope, without requiring a completed-result cache.
- **Stale data:** Previously successful data not verified by the latest requested refresh.
- **Uncertain write outcome:** A write that may have executed remotely and must not be blindly repeated.

## Bug Conditions and Current Behavior

### Confirmed mechanisms

1. **Receiving refresh within cache lifetime:** An unchanged refresh can return cached rows and badges without contacting NetSuite.
2. **Overlapping grid loads:** A queued load can return before reconciliation completes. Direct shared-grid reload methods can bypass coordination and allow an older response to overwrite a newer result.
3. **Failed records refresh:** Higher-level error handling can convert failure into empty rows and count zero.
4. **Failed badge refresh:** Previous totals can remain visible without indicating failure.
5. **Avoidable Packing requests:** Print-status selection recalculates unchanged global totals. Packing does not use the grid-cache decorators applied to Receiving.
6. **Capacity assumptions:** The interim limiter's deployment budget must be verified across all NetSuite-calling processes.

### Unconfirmed contributors

- Broad aggregate query execution cost.
- Additional lifecycle-triggered grid loads.
- Remote execution continuing after local cancellation or timeout.
- IIS cold starts or browser/SignalR rendering delays.

These are investigation targets, not established root causes. Auto-fulfillment investigation is excluded.

## 1. Reliable Grid Load Ordering

### 1.1 Unified load behavior

All server-side load entry points in the touched shared grid abstractions SHALL follow consistent request ordering, result publication, and completion semantics.

### 1.2 Latest applicable result

A result SHALL update the visible grid only if its captured scope and request generation remain applicable.

### 1.3 Awaitable reconciliation

An awaited refresh SHALL NOT report successful completion merely because its work was queued. A superseded outcome SHALL be distinguishable from successful reconciliation.

### 1.4 Bounded pending work

Repeated overlapping requests SHALL NOT create an unbounded backlog. The system SHALL retain only work still needed to satisfy the latest applicable request.

### Acceptance criteria

- **AC1:** WHEN an older read completes after a newer applicable read, THEN the older result SHALL NOT overwrite the newer rows or count.
- **AC2:** WHEN refresh work is queued, THEN its awaiting caller SHALL receive completion only after that work is reconciled, failed, cancelled, or explicitly superseded.
- **AC3:** WHEN repeated refresh/filter/page actions overlap, THEN obsolete intermediate requests SHALL NOT accumulate without bound.

## 2. Explicit Refresh Freshness

### 2.1 Fresh-read semantics

An explicit refresh SHALL request fresh rows and badge totals for its applicable scope rather than treating an existing completed cache entry as sufficient.

### 2.2 Preserve normal caching

Ordinary navigation and unchanged non-explicit loads MAY continue using the existing short-lived cache.

### 2.3 Avoid refresh storms

Equivalent overlapping refreshes SHALL share work where safe. Freshness support SHALL NOT require globally evicting unrelated users' cached pages.

### Acceptance criteria

- **AC4:** WHEN an operator refreshes a populated Receiving grid within the cache lifetime, THEN reconciliation SHALL NOT be satisfied solely by its previous completed cache entry.
- **AC5:** WHEN equivalent refreshes overlap, THEN the system SHALL avoid duplicate upstream work without serving a pre-mutation result as post-mutation reconciliation.
- **AC6:** WHEN one grid is explicitly refreshed, THEN unrelated users or scopes SHALL NOT lose cached results solely because of that action.

## 3. Honest Failure and Staleness Feedback

### 3.1 Failed reads are not empty success

Failed or cancelled records reads SHALL NOT be published as successful empty datasets.

### 3.2 Same-scope failure

If a refresh fails for the same scope, the grid SHOULD retain its last successful rows and count, clearly marked stale or refresh-failed.

### 3.3 Changed-scope failure

Previously loaded rows SHALL NOT appear to belong to a newly selected filter, subsidiary, or authorization scope.

### 3.4 Badge failure

Retained badge totals SHALL visibly indicate unsuccessful reconciliation until a successful refresh replaces them.

### 3.5 Post-write distinction

A successful print-count or transaction write followed by a failed read SHALL be reported as a reconciliation failure, not a failed write or an instruction to repeat the mutation.

### Acceptance criteria

- **AC7:** WHEN a populated grid's same-scope refresh receives an exhausted 429 failure, THEN prior valid rows SHALL NOT become a successful "No records" result.
- **AC8:** WHEN a changed-scope read fails, THEN the UI SHALL NOT present old-scope rows as current.
- **AC9:** WHEN badge refresh fails after previous success, THEN retained totals SHALL be visibly stale.
- **AC10:** WHEN a write succeeds but reconciliation fails, THEN the operator SHALL be told the write succeeded and refresh is needed.

## 4. Reduce Unnecessary NetSuite Reads

### 4.1 Stable badge scope

Changing only Packing's selected print-status view SHALL NOT recalculate global badge totals whose input scope has not changed.

### 4.2 Required reconciliation

Initial loading, explicit refresh, successful mutations, and changes to badge-defining scope SHALL continue to trigger appropriate badge reconciliation.

### 4.3 Safe coalescing

Equivalent overlapping reads SHALL share their in-flight outcome where introduced, including a shared failure. Exceptions SHALL NOT become persistent cached results.

### 4.4 Scope isolation

Coalescing and caching SHALL preserve user, subsidiary, authorization, filter, sort, paging, and mutation-generation boundaries.

### 4.5 Cancellation isolation

Cancelling one caller's wait SHALL NOT cancel another caller's still-valid shared read.

### Acceptance criteria

- **AC11:** WHEN Packing's print-status selection changes without a mutation or badge-scope change, THEN no additional badge query SHALL be issued solely because of that selection.
- **AC12:** WHEN equivalent reads are deliberately overlapped, THEN one shared upstream operation SHALL serve them where coalescing applies.
- **AC13:** WHEN a shared operation fails, THEN its overlapping callers SHALL receive that outcome without each starting a serial retry chain.
- **AC14:** WHEN callers differ in authorization or subsidiary scope, THEN they SHALL NOT share protected results.

## 5. Read Cancellation and Admission Behavior

### 5.1 End-to-end cancellation

Touched read paths SHALL propagate cancellation through component, feature handler, MediatR, integration/cache, paging, and transport boundaries.

### 5.2 Obsolete queued reads

A read cancelled before admission SHALL NOT subsequently send a NetSuite request.

### 5.3 Bounded admission wait

Interactive admission waits SHALL have an explicit finite deadline independent of HTTP timeout. The confirmed design sets a configurable 10-second default for applicable interactive reads.

### 5.4 Cancellation feedback

Expected supersession/disposal cancellation SHALL NOT produce a misleading error toast.

### 5.5 Write safety

Read-cancellation improvements SHALL NOT alter uncertain-write reconciliation or permit unsafe automatic write retries.

### Acceptance criteria

- **AC15:** WHEN a queued read is cancelled, THEN it SHALL complete without later sending its request.
- **AC16:** WHEN admission exceeds its configured deadline, THEN the caller SHALL receive a distinct busy outcome.
- **AC17:** WHEN a component is disposed or a read is superseded, THEN its late result SHALL NOT update the obsolete UI.
- **AC18:** WHEN a write times out or has an uncertain outcome, THEN this change SHALL NOT automatically repeat that write.

## 6. Verify Capacity and Measure Latency

### 6.1 Deployment inventory

Rollout SHALL document effective NetSuite limiter settings and all known NetSuite-calling processes, including deployment overlap and other integrations.

### 6.2 Capacity budget

The documented combined request budget SHALL fit within the fixed five-slot limit, with explicit headroom assumptions. Independent hosts SHALL NOT each be configured as though they own all five slots.

### 6.3 Timing separation

Diagnostics SHALL distinguish admission wait, HTTP attempt duration/count, retry delay, total records duration, and click-to-visible-rows.

### 6.4 Comparable measurements

Baseline and post-change measurements SHALL use the same deployed-version context, permissions, subsidiary, filters, page size, and representative dataset.

### 6.5 No unsupported performance claim

A speed improvement SHALL NOT be claimed solely from lower request counts or passing unit tests. No numeric end-to-end latency target is confirmed without baseline evidence.

### Acceptance criteria

- **AC19:** WHEN rollout is reviewed, THEN process counts, effective budgets, headroom assumptions, and deployed build identifiers SHALL be recorded.
- **AC20:** WHEN a slow load is investigated, THEN measurements SHALL distinguish queue pressure from remote execution and UI delay.
- **AC21:** WHEN performance results are reported, THEN cold/warm behavior and representative concurrent usage SHALL be identified separately.

## 7. Measurement-Gated SuiteQL Follow-up

### 7.1 SuiteQL optimization

Query changes SHALL be justified by measured execution cost and SHALL preserve eligible document IDs, counts, sorting, filtering, and pagination. Eligibility SHALL NOT be applied only after paginating a broader source set.

### Acceptance criteria

- **AC22:** WHEN a SuiteQL optimization is proposed, THEN current and candidate behavior SHALL be compared against equivalent fixtures and representative sandbox data before rollout.
- **AC24:** WHEN measurements do not justify a query change, THEN that change SHALL remain deferred rather than being included speculatively.

AC23 and original Requirements 7.2–7.3 are retired under the confirmed scope amendment.

## 8. Regression Validation

### 8.1 Defect demonstration

Deterministic tests SHALL demonstrate ordering, freshness, failure-state, and overlapping-read defects on unfixed behavior where feasible.

### 8.2 Fix verification

Tests SHALL verify corrected behavior using controlled overlapping operations, not only synchronously completed fakes.

### 8.3 Preservation verification

Existing integration and web suites SHALL remain passing. Shared-grid validation SHALL include affected non-Packing consumers.

### 8.4 Deployment verification

A controlled hosted verification SHALL confirm capacity assumptions and measured behavior. Production load generation requires separate authorization.

### Acceptance criteria

- **AC25:** WHEN concurrency tests execute, THEN they SHALL deliberately hold and release operations to force relevant overlap.
- **AC26:** WHEN the fix is validated, THEN paging, saved settings, selection, print reconciliation, and authorization isolation SHALL retain intended behavior.
- **AC27:** WHEN automated tests pass without hosted measurements, THEN reporting SHALL explicitly state that production latency remains unverified.

## In Scope

- Packing STR, Returns, and VRA/RTS grid behavior.
- Receiving PO, STR, and Returns grid behavior.
- Shared grid coordination and directly affected consumers.
- Explicit refresh freshness and failure feedback.
- Targeted read cancellation and coalescing.
- Capacity diagnostics and deployment verification.
- Evidence-gated SuiteQL follow-up.

## Out of Scope

- Increasing NetSuite concurrency licensing or indiscriminately raising local limits.
- Redis, distributed admission infrastructure, or routing all web traffic through the API.
- Broad cache replacement or arbitrary cache-lifetime increases.
- Fulfillment payload redesign, generic write-ledger redesign, or altered write-retry policy.
- Automatic default date restrictions.
- Authentication redesign, SAP/mobile feature changes, or unrelated architectural-debt cleanup.
- Unmeasured SQL rewrites, IIS changes, or production load tests.
- Auto-fulfillment investigation, optimization, disabling, or removal in this patch.

## Preservation Requirements

The change SHALL preserve:

1. Authorization and subsidiary isolation.
2. Remaining-line packing eligibility and printable-child eligibility.
3. Child receipt/fulfillment badge semantics, not source-document counts.
4. Bin/non-bin validation and payload consistency.
5. Authoritative pre-post validation and uncertain-write protections.
6. Existing safe write-retry classification.
7. Operator-controlled date filtering.
8. Pagination, stable collection references, saved settings, sorting, and selection.
9. Mutation-generation cache invalidation.
10. Existing application-layer and UI dispatch boundaries.
