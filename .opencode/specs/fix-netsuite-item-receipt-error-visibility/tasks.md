# NetSuite Item Receipt Error Visibility — Tasks Document

**Spec name:** `fix-netsuite-item-receipt-error-visibility`
**Mode:** BUGFIX
**Status:** Stage 1 code complete (T1–T6); Stage 2 gate pending (T7)
**Date:** 2026-10-06

All Stage 1 tasks are unconditional. Stage 3 tasks are **conditional** and must not begin
until T7 produces a classified error signal.

---

## Phase 1 — Stage 1: Observability (unconditional)

### T1 — Fix `QuantityOpen` self-reference ✅ DONE

- **Dependencies:** none
- **Files:** `Application.DataTransferObjects/Transactions/Receiving/ItemReceiptDTO.cs` (property `QuantityOpen`, currently line 68)
- **Requirements:** 1.4, 2.6, 3.5
- **Acceptance Criteria:**
  - `QuantityOpen` subtracts the received quantity from the planned quantity instead of
    from itself.
  - Evaluating the property returns a value and does not throw.
  - The surrounding DTO compiles unchanged.
- **Testing Strategy:** manual evaluation of the property against a mapped
  `ItemReceiptLineDTO` sample; confirmed during T6.

### T2 — Surface HTTP status code and bounded response body in the exception ✅ DONE

- **Dependencies:** none (may run parallel to T1)
- **Files:** `Integration.NS/Services/NetSuiteApiClientService.cs` — `MakeRequestOAuth1` (lines 466-469); evaluate `MakeRequest` for the same defect
- **Requirements:** 1.1, 1.2, 2.1, 2.2
- **Acceptance Criteria:**
  - Every non-2xx response throws an exception whose message contains the numeric status
    code and status reason.
  - When a parsed detail exists, it is appended after the status.
  - When no detail can be parsed, the message contains a raw-body excerpt of at most
    2000 characters.
  - No exception is ever constructed with a null or empty message.
  - On the Bearer_Path, existing `NetSuiteErrorResponse.DisplayString` messages are
    preserved; the status-plus-body fallback applies only when `DisplayString` is null
    or empty, and it is derived from the already-read response string (no second
    `ReadFromJsonAsync` content read, which could throw `JsonException` and mask the
    status).
  - The commented-out status-code line at 468 is removed as superseded.
- **Testing Strategy:** reproduce the reported STR failure; assert the toast no longer
  shows "Exception of type 'System.Exception' was thrown".

### T3 — Extend `GetNetSuiteErrorDetail` with the RESTlet error shape ✅ DONE

- **Dependencies:** T2 (message assembly consumes the parser result)
- **Files:** `Integration.NS/Services/NetSuiteApiClientService.cs` — `GetNetSuiteErrorDetail` (lines 472-518)
- **Requirements:** 1.2, 2.3, 3.2
- **Acceptance Criteria:**
  - `{ "error": { "message": "..." } }` returns the message value.
  - `{ "error": { "title": "..." } }` returns the title value as fallback.
  - A top-level `title` or `message` property is accepted as a final fallback.
  - The existing `o:errorDetails[].detail` and `error`-as-string formats continue to
    parse identically.
  - Malformed JSON still returns null rather than throwing.
- **Testing Strategy:** supply representative NetSuite error bodies through a failing
  request; confirm the parsed text reaches the exception message.

### T4 — Inject `ILogger<T>` and log failed NetSuite responses ✅ DONE

- **Dependencies:** T2
- **Files:** `Integration.NS/Services/NetSuiteApiClientService.cs` — constructor and both HTTP request methods
- **Requirements:** 1.3, 2.4
- **Acceptance Criteria:**
  - The service resolves `ILogger<NetSuiteApiClientService>` through constructor
    injection, matching the handler-level convention.
  - Every non-2xx response emits a warning containing the request URL, status code, and
    response body.
  - The Authorization header and OAuth tokens are never logged.
  - Successful responses are not logged at warning level.
- **Testing Strategy:** reproduce a failure and confirm the log entry contains URL,
  status, and body.

### T5 — Preserve inner exceptions and remove the dead filter in `PostItemReceipt` ✅ DONE

- **Dependencies:** T2
- **Files:** `Integration.NS/Implementations/Transactions/ReceivingIntegration.cs` (lines 607-624)
- **Requirements:** 2.5, 3.4
- **Acceptance Criteria:**
  - Each rethrow passes the original exception as `inner`, preserving the stack chain.
  - When payload construction throws before any post task is created
    (`tasks.Count == 0`), that exception is rethrown instead of being swallowed into a
    `true` return.
  - The final aggregate throw carries the collected exception(s) as its inner
    exception (e.g. an `AggregateException`), preserving the original stack chain.
  - The unreachable "Empty response from NetSuite API" filter is removed.
  - The user-facing toast prefix format is unchanged.
  - Aggregated multi-line failure messages still include each underlying message.
- **Testing Strategy:** confirm the toast text is unchanged in format while the log
  retains the full exception chain.

### T6 — Build validation and manual regression pass ✅ DONE

- **Dependencies:** T1, T2, T3, T4, T5
- **Files:** none (verification only)
- **Requirements:** 3.1, 3.2, 3.3, 3.4, 3.5
- **Acceptance Criteria:**
  - The full solution builds with no new warnings.
  - Scenarios T1-T5 of the design testing matrix pass.
- **Testing Strategy:** execute the design testing matrix scenarios T1 through T5.
- **Verification (2026-10-06):** `Application.DataTransferObjects`, `Integration.NS`,
  `Application.UseCases`, `Api.CoreWebAPI`, and `Web.BlazorServer` all build with 0
  errors. The pre-change full-solution run (`WMS.slnx`) confirmed the single failure
  was unrelated (a Mobile.MAUI error with no matching error line in the log; no
  Integration.NS errors), and the post-fix `Integration.NS` rebuild plus the full
  dependent closure (`DTO → Integration.NS → UseCases → API/Blazor`) all succeed.
  No new warnings were introduced — the two nullable warnings in the touched region
  (`NetSuiteApiClientService.cs` lines 484/486) are pre-existing on the success path.
  Design matrix T1–T5 conformance was re-verified by inspection: toast text comes from
  `AppActionFactory` (`"{ActionName} failed: {ex.Message}"`) so the toast prefix format
  is unchanged; the multi-line aggregate join (`"\n\n"`) is unchanged; no Authorization
  header or token values flow into any log call (only method, URL, status, payload,
  and body).

---

## Phase 2 — Stage 2: Diagnostic gate (manual, no code)

### T7 — Capture and classify the real NetSuite rejection

- **Dependencies:** T6 deployed to the target environment
- **Files:** none
- **Requirements:** 1.3, 1.5, 1.6 (validation)
- **Acceptance Criteria:**
  - One failing STR receipt is reproduced after the Stage 1 deploy.
  - The resulting toast text is recorded verbatim.
  - The NetSuite Script Execution Log entry for script 1853 / deployment 1 is captured,
    including the exception detail.
  - The observation is classified into exactly one branch: A (bin reference), B
    (unknown source line), C (fulfillment reference), D (authorization), or E (request
    never reached the restlet).
- **Testing Strategy:** not applicable — this task produces the diagnostic evidence
  consumed by Phase 3.

---

## Phase 3 — Stage 3: Conditional payload fix (blocked on T7)

Implement only the branch that T7 confirms. All other Stage 3 tasks are deferred to a
follow-up spec.

### T8 — Branch A: align `binNumber` (only if T7 classifies as A)

- **Dependencies:** T7 classified as A
- **Files:** `Integration.NS/Implementations/Transactions/ReceivingIntegration.cs` (`CreateTOJson`, line 876; and `CreateReturnsJson` line 840 if the same restlet family requires it)
- **Requirements:** 1.6, 2.8
- **Acceptance Criteria:**
  - `binNumber` is sent in the form proven to work on the failing flow.
  - Bin-less detail lines continue to omit the field rather than sending null.
- **Testing Strategy:** post a sandbox receipt using bins and confirm it succeeds.

### T9 — Branch B: source `orderLine` from `transferorderitemlineid` (only if T7 classifies as B)

- **Dependencies:** T7 classified as B
- **Files:** `Integration.NS/Implementations/Transactions/ReceivingIntegration.cs` — `GetItemReceiptItemFulfillmentLinesAsync` (line 448) and the `IF_TO_LINE_ID` constant (lines 425-439)
- **Requirements:** 1.5, 2.7, 3.6
- **Acceptance Criteria:**
  - `LineNumber` resolves from the Item Fulfillment line's own `transferorderitemlineid`.
  - The correlated `IF_TO_LINE_ID` subquery is removed.
  - For correctly-linked fulfillments the resolved value is identical to the previous
    implementation.
  - A fulfillment whose previous resolution returned `0` no longer produces `orderLine: 0`.
- **Testing Strategy:** load lines for a known-good IF and diff the resolved line ids
  against the pre-change query output; then post a sandbox receipt.

### T10 — Branch C: reject missing fulfillment ids (only if T7 classifies as C)

- **Dependencies:** T7 classified as C
- **Files:** `Web.BlazorServer/Components/Pages/Transaction/Receiving/ItemReceiptCreatePage.razor.cs` (line 75)
- **Requirements:** 1.1 (root cause class), 2.9
- **Acceptance Criteria:**
  - The `?? -1` fallback is removed.
  - When no fulfillment id is present the submit is refused with an actionable message.
  - The `fulfillmentId` value actually sent in the payload is included in the Stage 1
    warning log so the observation is classified into exactly one branch.
- **Testing Strategy:** open the page without a fulfillment id and confirm a clear
  validation message instead of a NetSuite rejection; with an id present, confirm the
  logged payload contains that id.

### T11 — Branch D: authorization remediation (only if T7 classifies as D)

- **Dependencies:** T7 classified as D
- **Files:** none — NetSuite administrator action on the integration role or token
- **Acceptance Criteria:** the receipt posts successfully after the role or token is
  corrected.
- **Testing Strategy:** repeat the receipt post after remediation.

---

## Definition of Done

- All Stage 1 tasks (T1-T6) are complete and merged.
- The reported failing receipt has been reproduced at least once with a toast that shows
  the real NetSuite error instead of the generic message.
- T7 has produced a recorded branch classification backed by the Script Execution Log.
- Either the matched Stage 3 task is complete and verified against a sandbox receipt, or
  the classification is recorded as non-payload (Branch D) and handed to a NetSuite
  administrator.
- Unmatched Stage 3 branches are recorded as deferred follow-up work.
- Requirements 3.1 through 3.5 are verified as preserved.
