# Scan-First Transaction Selection — Tasks

| Metadata | Value |
|---|---|
| Date | 2026-10-27 |
| Status | Confirmed |
| Mode | FEATURE |
| Scope | Receiving PO, TO, Returns; Packing TO, Returns, Vendor Return Authorization |
| Spec | [design.md](design.md) |
| Related | None found |

## Goal

Allow operators to select one eligible Receiving or Packing transaction by barcode scan or manual transaction-number entry, without automatically loading pending lists. Preserve existing downstream transaction workflows.

## Architecture summary

Mobile selection page → operation-specific API lookup → Application query/handler and existing transaction source → existing mobile detail/next-step screen.

## Technical context and constraints

- The solution contains `Mobile.MAUI`, `Api.CoreWebAPI`, `Application.UseCases`, and `Integration.NS.Tests` (`WMS.slnx:1-39`).
- Existing mobile list caller/controller paths are identified below. The `.claude` guides referenced by `CLAUDE.md:19-30` were unavailable in the inspected location; re-check them and the current tree before implementation.
- Five corresponding Application handlers were located but not fully inspected. Confirm their actual source and conventions before editing.
- Do not change Trip Ticket, Inventory Counting, worksheet creation, fulfillment sub-selection, detail/item pages, transaction status/save behavior, or pending-list API behavior for other consumers.
- Server-side authorization and eligibility are mandatory; barcode possession is not authorization.
- No schema changes are assumed. Do not create or name unverified handler/DTO paths without investigation.

## Phase 1 — Verify contracts and test surface

### 1.1 Confirm identifiers, lookup rules, and navigation data

For all six operations, inspect actual barcode formats, existing Mobile event handlers, API actions, Application queries/handlers, source calls, and destination routes. Verify identifier uniqueness, expected transaction type, operator/subsidiary constraints, eligibility/status rules, and fields required for downstream navigation. Inspect the five unverified handlers listed in `design.md` before deciding whether a lookup can be added by extending an existing query or needs a new request.

**Completion criteria:** A contract matrix exists for all six operations. Any transaction type without a safe unique identifier or enforceable lookup is held out until resolved. _Requirements: 3.1–3.6, 4.1_

### 1.2 Audit consumers and tests

Search repository consumers of the six pending-list APIs and identify reusable single-record lookups. Inspect `Integration.NS.Tests` for relevant Receiving/Packing tests and establish whether API/controller tests exist. Re-check current build/test instructions and project conventions.

**Completion criteria:** Test surface, exact commands, and endpoint consumers are recorded; no list endpoint is removed or changed without a verified usage/safety case. _Requirements: 6_

## Phase 2 — Implement server-side single-transaction lookups

### 2.1 Add or reuse operation-specific lookup contracts

Implement one safe lookup path for each verified transaction operation in `Api.CoreWebAPI/Controllers/Receiving/` and `Api.CoreWebAPI/Controllers/Packing/`, backed by the appropriate Application request/handler and source integration. Reuse existing single-transaction capability where verified. Return only navigation/detail-required data and retain existing API result conventions.

Operations: Receiving PO, Receiving TO, Receiving Returns, Packing TO, Packing Returns, Packing Vendor Return Authorization.

**Completion criteria:** Each contract resolves only the expected transaction type, applies server-side authorization/subsidiary/status/eligibility checks, and has well-defined no-match/denial/error outcomes. _Requirements: 3.1–3.6, 4.1_

### 2.2 Test API/Application lookup outcomes

Cover eligible match, not found, wrong type, ineligible status, unauthorized/subsidiary mismatch, and relevant source failure for each operation using verified existing test patterns. Do not imply test coverage for an endpoint unless a test asserts its behavior.

**Completion criteria:** Supported automated tests demonstrate eligibility and denial behavior; uncovered test infrastructure limitations are documented. _Requirements: 4.1_

## Phase 3 — Convert Receiving mobile selectors

### 3.1 Receiving PO

Update `Mobile.MAUI/Components/Pages/Receiving/PurchaseOrder/PurchaseOrderView.razor` and `.razor.cs`. Remove first-render pending-list loading and list-backed search/scan matching; provide scan/manual submission to the PO lookup, progress and recoverable feedback, and existing successful navigation.

**Completion criteria:** Opening PO makes no pending-list request; a valid lookup opens the existing PO workflow with required context. _Requirements: 3.1, 4.1_

### 3.2 Receiving TO

Update `Mobile.MAUI/Components/Pages/Receiving/TransferOrder/TransferOrderView.razor` and `.razor.cs`. Remove first-render and pagination/list-backed selection behavior from this selector; use the verified TO lookup and preserve current destination behavior.

**Completion criteria:** Opening TO makes no pending-list request; a valid lookup opens the existing TO workflow with required context. _Requirements: 3.2, 4.1_

### 3.3 Receiving Returns

Update `Mobile.MAUI/Components/Pages/Receiving/Returns/ReturnsView.razor` and `.razor.cs`. Replace list-backed selection with the verified Returns lookup and preserve existing destination behavior.

**Completion criteria:** Opening Returns makes no pending-list request; a valid lookup opens its existing workflow with required context. _Requirements: 3.3, 4.1_

## Phase 4 — Convert Packing mobile selectors

### 4.1 Packing TO

Update `Mobile.MAUI/Components/Pages/Packing/TransferOrder/TransferOrderView.razor` and `.razor.cs`. Replace eager list loading and local scan matching with the verified lookup.

**Completion criteria:** Opening TO makes no pending-list request; a valid lookup opens the existing Packing TO workflow with required context. _Requirements: 3.4, 4.1_

### 4.2 Packing Returns

Update `Mobile.MAUI/Components/Pages/Packing/Returns/ReturnsView.razor` and `.razor.cs`. Replace eager list loading and local scan matching with the verified lookup.

**Completion criteria:** Opening Returns makes no pending-list request; a valid lookup opens its existing Packing workflow with required context. _Requirements: 3.5, 4.1_

### 4.3 Packing Vendor Return Authorization

Update `Mobile.MAUI/Components/Pages/Packing/VendorReturnAuthorization/VendorReturnAuthorizationView.razor` and `.razor.cs`. Replace eager list loading and local scan matching with the verified lookup.

**Completion criteria:** Opening Vendor Return Authorization makes no pending-list request; a valid lookup opens its existing workflow with required context. _Requirements: 3.6, 4.1_

### 4.4 Common scanner and submission behavior

Across the six selectors, normalize input consistently, ignore empty scans, prevent overlapping submissions, show progress, restore input after failure, and correctly unsubscribe scanner events on disposal. Keep operation-specific result messaging and routing.

**Completion criteria:** Empty/repeated scans do not cause unnecessary or duplicate lookups/navigation; retry is possible after recoverable failure. _Requirements: 3.1–3.6, 4.1_

## Phase 5 — Regression and performance verification

### 5.1 Verify selector and lookup behavior

Test no eager list call, barcode and manual entry, valid navigation, not-found/wrong-type/ineligible outcomes, network/server failure and retry, loading state, and duplicate submission. Use component tests only if supported by the repository; otherwise document and execute a focused manual plan alongside automated server tests.

**Completion criteria:** Tests/build supported by repository guidance pass; each outcome has observable verification evidence. _Requirements: 3.1–3.6, 4.1_

### 5.2 Verify preservation and measure improvement

Confirm item/detail line loading, item scanning, saving, and status behavior remain unchanged. Confirm Trip Ticket, Inventory Counting, worksheet creation, Receiving fulfillment sub-selection, and other list endpoint consumers are unaffected. Compare initial selector request count and time-to-open-transaction before/after.

**Completion criteria:** No in-scope page fetches a pending list on entry; excluded workflows remain unchanged; performance comparison is recorded. _Requirements: 6_

## Optional work

Add a browse-pending fallback only if operator evidence later establishes the need; it is excluded from the confirmed scope. _Requirements: None (out of scope)._ 
