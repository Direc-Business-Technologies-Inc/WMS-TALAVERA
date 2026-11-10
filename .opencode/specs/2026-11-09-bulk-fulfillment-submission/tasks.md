# Bulk Fulfillment Submission — Tasks

- **Date:** 2026-11-09
- **Status:** Confirmed
- **Mode:** FEATURE
- **Scope:** Web RTS bulk selection and NetSuite BF request creation.
- **Spec:** [design.md](design.md)
- **Related:** [requirements.md](requirements.md)
- **Goal:** Submit one BF record containing business date, Submitted `1`, and unique selected RTS references.

The user confirmed these tasks before authorizing writing. This package is a specification, not implementation authorization. Earlier conversational dates were corrected to the writing date.

## Global Constraints and Context

Preserve Blazor component → feature handler → MediatR command → Supplier Return integration → NetSuite client. Relevant application/Web/integration projects target .NET 8; existing test projects provide xUnit/WireMock and bUnit/NSubstitute. Follow `.claude` UI, async, authorization, error/result, and integration conventions; preserve documented debts. No SAP/Mobile change or database migration is required by this design.

Require at least two unique RTS IDs. Resolve date/status server-side. Do not create IFs, allocate inventory, calculate quantities, introduce a worker/queue/local BF schema, or broaden ambiguous-write retries. Proposed new paths are not existing files. Recheck cited line numbers and current user changes before implementation. Do not change branches or commit without a separate request.

## Phase A — Integration Prerequisites

### Task 1 — Verify NetSuite creation contract

1. Verify REST creation of `customrecord_bulk_fulfillment`.
2. Establish actual date, status-reference, and RTS multi-select serialization for `custrecord_bf_date`, `custrecord_bf_status`, and `custrecord_bf_return_to_supplier_ids`.
3. Verify role permissions, mandatory fields, target-environment Submitted `1`, and account customizations.
4. Establish creation receipt/ID behavior.
5. Document repeated-RTS submission behavior.

**Complete when:** sanitized schema/request/response evidence establishes the contract, logical arrays are not confused with verified wire arrays, and duplicate limitations are recorded. Sandbox writes require explicit authorization; no production test writes.

_Requirements: 5.4.1–5.4.6, 5.6.1–5.6.3._

### Task 2 — Resolve authorization and business date

Inspect `Web.BlazorServer/Handlers/Implementations/Transaction/SupplierReturn/SupplierReturnHandler.cs:21–51`, `.claude/auth.md`, and existing configuration/date conventions. Exact owning configuration/query files are investigation outputs, not guessed locations.

1. Identify applicable presentation policy and trusted user scope.
2. Define authoritative accessible-RTS identity validation.
3. Confirm business timezone and identify existing clock/configuration abstractions.

**Complete when:** policy, trusted scope source, query boundary, and timezone are documented; no browser date/status/scope is trusted. Unresolved decisions block posting rather than being guessed.

_Requirements: 5.3.1–5.3.4, 5.4.2–5.4.3, 6.3._

**Checkpoint A:** wire format, receipt, authorization, validation query, and timezone are established.

## Phase B — Contracts and NetSuite Support

### Task 3 — Add request and result contracts

**Proposed owning area:** existing Supplier Return DTO area under `Application.DataTransferObjects`; choose exact files consistent with surrounding naming.

1. Add ID-only request DTO.
2. Add typed Submitted/Rejected/Uncertain result, BF ID when available, and suitable message.
3. Represent confirmed acceptance with no recoverable reference without treating it as rejection.
4. Follow existing result conventions; do not collapse to boolean.

**Complete when:** contracts exclude quantities, allocations, arbitrary status/browser date, and distinguish all outcomes under test.

_Requirements: 5.1.2–5.1.3, 5.4.3–5.4.6, 5.5.2–5.5.5, 5.6.1._

### Task 4 — Add creation-response support

**Existing files:** `Integration.NS/Services/NetSuiteApiClientService.cs:471–525,567–618`; `Integration.NS/Resilience/NetSuitePipelines.cs:73–84`. Locate the client interface, implementations, and fakes before altering contracts.

1. Add an additive narrowly scoped response capability exposing verified receipt metadata/body.
2. Preserve existing generic callers and empty-success handling.
3. Parse the ID from Task 1's verified response; test valid absolute/relative locations and malformed/missing references if Location is used.
4. Preserve cancellation and 429-only write-safe retries.
5. Map accepted-without-reference truthfully; never issue another POST to recover the reference.

**Complete when:** callers remain compatible, receipt parsing is covered, and no network/timeout/5xx retry is added.

_Requirements: 5.4.5, 5.5.2, 5.6.1–5.6.3, 6.4._

### Task 5 — Implement BF integration

**Existing files:**

- `Application.UseCases/Repositories/Integration/Transaction/SupplierReturn/ISupplierReturnIntegration.cs:12–26`
- `Integration.NS/Implementations/Transactions/SupplierReturnIntegration.cs:243–259`
- `Integration.NS/Implementations/Others/Caching/CachedSupplierReturnIntegration.cs:13–21,51–68`

1. Add dedicated interface/concrete/decorator operation.
2. Validate authoritative RTS identities within trusted scope before writing.
3. Add typed payload using verified wire contract: business date, Submitted `1`, unique references; omit error field.
4. Create one BF and map result/receipt.
5. Forward through decorator without falsely marking RTS records fulfilled.

**Complete when:** invalid/inaccessible selections cause no create, one logical submission creates one BF apart from permitted 429 retries, no IF operation is called, and payload/outcome tests pass.

_Requirements: 5.3.3–5.3.4, 5.4.1–5.4.6, 5.6.1–5.6.3, 6.1–6.4._

## Phase C — Application and Presentation

### Task 6 — Add application command

**Pattern:** `Application.UseCases/Commands/Transaction/SupplierReturn/CreateSupplierReturnCmd.cs:12–19`.

**Proposed file:** `Application.UseCases/Commands/Transaction/SupplierReturn/SubmitBulkFulfillmentCmd.cs`.

1. Consume ID request and produce typed submission result.
2. Reject nonpositive IDs, deduplicate, and require two unique IDs.
3. Coordinate authoritative validation and dedicated integration operation.
4. Preserve cancellation where supported and established expected-failure conventions.

**Complete when:** duplicates cannot satisfy minimum two, invalid selections do not create BF records, no individual RTS/IF command is invoked, and no local transaction is portrayed as remote rollback.

_Requirements: 5.1.2–5.1.3, 5.3.2–5.3.4, 5.4.1, 5.4.6, 6.3._

### Task 7 — Extend presentation dispatcher

**Existing files:**

- `Web.BlazorServer/Handlers/Repositories/Transaction/SupplierReturn/ISupplierReturnHandler.cs:9–23`
- `Web.BlazorServer/Handlers/Implementations/Transaction/SupplierReturn/SupplierReturnHandler.cs:21–51`

Add submission method, enforce Task 2's policy, resolve trusted scope, dispatch command, and preserve result distinctions.

**Complete when:** bypassing the button does not bypass authorization, dispatcher remains thin, and components do not directly access MediatR/repositories/clients.

_Requirements: 5.3.1, 5.5.2–5.5.5, 5.6.1, 6.3._

### Task 8 — Activate confirmation and submission

**Existing files:**

- `Web.BlazorServer/Components/Pages/Transaction/SupplierReturn/Components/BulkFulfillConfirmation.razor:27–41`
- `Web.BlazorServer/Components/Pages/Transaction/SupplierReturn/Components/SupplierReturnsDataGrid.razor:15–16`
- `Web.BlazorServer/Components/Pages/Transaction/SupplierReturn/Components/SupplierReturnsDataGrid.razor.cs:49–72,88–92`

1. Enable Submit Request/Cancel and truthful submission wording.
2. Display references/count; return confirmation/cancellation.
3. Keep immutable snapshot and in-flight guard spanning dialog and request.
4. Ensure search filters display only; distinguish selected versus matching counts.
5. On acceptance, show receipt/appropriate missing-reference warning, close, and clear selection.
6. On rejection, preserve selection, restore controls, and show actionable detail.
7. On uncertainty, warn that creation may have occurred and require verification before resubmission; do not automatically retry.

**Complete when:** minimum-two rule, cancel/no-write, no concurrent submission, truthful wording, and existing grid behavior are covered.

_Requirements: 5.1.1–5.1.4, 5.2.1–5.2.4, 5.5.1–5.5.5, 5.6.1–5.6.3, 6.2–6.3._

**Checkpoint B:** UI-to-integration flow works against a controlled test double and makes no IF writes.

## Phase D — Verification

### Task 9 — Focused automated coverage

**Existing projects:** `Integration.NS.Tests/Integration.NS.Tests.csproj`, `Web.BlazorServer.Tests/Web.BlazorServer.Tests.csproj`. Existing client precedent: `Integration.NS.Tests/Services/NetSuiteApiClientServiceResilienceTests.cs`.

1. Test payload/date boundaries, ID normalization, minimum two, access validation.
2. Test receipt/empty-success/missing-reference paths and rejected/uncertain outcomes.
3. Verify 429 policy and no ambiguous-write retries.
4. Test dialog/cancel/search/busy guard and selection lifecycle.
5. Test presentation permission/access and absence of IF calls.
6. Locate a suitable existing test host for application commands after checking references; do not add a project by default.

**Complete when:** design properties have automated coverage or explicit sandbox checks, relevant regressions pass, and tests use no production writes/credentials.

_Requirements: 5.1.1–5.1.4, 5.2.1–5.2.4, 5.3.1–5.3.4, 5.4.1–5.4.6, 5.5.1–5.5.5, 5.6.1–5.6.3, 6.1–6.4._

### Task 10 — Sandbox and regression verification

1. Build and run relevant existing tests in the documented environment.
2. With explicit authorization, submit controlled sandbox RTS records.
3. Verify one BF, correct business date, initial Submitted `1`, exact references, omitted error field, and receipt handling.
4. Verify no additional WMS IF writes and preserve individual RTS workflows.
5. Record results and external duplicate-handling limitations.
6. After code implementation, run `graphify update .` as repository instructions require.

```powershell
dotnet build "WMS.slnx"
dotnet test "Integration.NS.Tests/Integration.NS.Tests.csproj"
dotnet test "Web.BlazorServer.Tests/Web.BlazorServer.Tests.csproj"
```

**Complete when:** actual build/test results and authorized sandbox evidence are recorded; no production test writes or invented passing results/counts; known limitations remain explicit.

_Requirements: 5.4.1–5.4.6, 5.5.2–5.5.3, 5.6.1–5.6.3, 6.1–6.4._

## Final Checkpoint

All three documents agree on submission-only ownership, minimum two unique RTS IDs, Submitted `1`, business date, exact confirmed references, truthful outcomes, no ambiguous retry, and no IF creation. Implementation starts only after rechecking current repository state and resolving prerequisite contract decisions. No implementation, branch change, or commit is part of writing this package.
