# Scan-First Transaction Selection — Design

| Metadata | Value |
|---|---|
| Date | 2026-10-27 |
| Status | Confirmed |
| Mode | FEATURE |
| Scope | Receiving PO, TO, Returns; Packing TO, Returns, Vendor Return Authorization |
| Related | None found |

## 1. Decision record

### Problem

Six mobile selection pages fetch pending transaction lists on entry. Their search and barcode handlers filter those in-memory lists, making list retrieval a prerequisite for selecting a transaction.

### Decisions

- Keep the existing selection pages but replace automatic list loading with scan/manual identifier entry and a server-side single-transaction lookup.
- Use operation-specific lookup contracts so transaction type and eligibility rules remain explicit.
- On successful lookup, navigate through the existing workflow using the required transaction context.
- Leave existing pending-list endpoints available unless a usage audit establishes they are unused and safe to change.

### Trade-offs

Operators will no longer discover work by browsing a pending list by default. A browse fallback is excluded from this feature. Identifiers, authorization, and eligibility must be verified independently for every operation.

## 2. What already exists

- Receiving PO calls `/Receiving/PurchaseOrder/PendingReceipt`; it loads on first render and filters in-memory data for text/barcode search (`Mobile.MAUI/Components/Pages/Receiving/PurchaseOrder/PurchaseOrderView.razor.cs:26-45,50-64,78-119`). API controller: `Api.CoreWebAPI/Controllers/Receiving/PurchaseOrderController.cs:14-25`.
- Receiving TO calls `/Receiving/TransferOrder/PendingReceipt` and pages through results; search filters loaded data (`Mobile.MAUI/Components/Pages/Receiving/TransferOrder/TransferOrderView.razor.cs:34-55,61-113,115-155`). API controller: `Api.CoreWebAPI/Controllers/Receiving/TransferOrderController.cs:15-26`; its located query handler uses limit/offset (`Application.UseCases/Queries/Transaction/Receiving/NS/TransferOrder/GetTransferOrdersQry.cs:9-29`).
- Receiving Returns calls `/Receiving/Returns/PendingReceipt` and filters the loaded list (`Mobile.MAUI/Components/Pages/Receiving/Returns/ReturnsView.razor.cs:28-45,48,64-82`). API controller: `Api.CoreWebAPI/Controllers/Receiving/ReturnsController.cs:15-27`.
- Packing TO calls `/Packing/TransferOrder/PendingFulfillment` and matches scans against loaded data (`Mobile.MAUI/Components/Pages/Packing/TransferOrder/TransferOrderView.razor.cs:26-45,61-83`). API controller: `Api.CoreWebAPI/Controllers/Packing/TransferOrderController.cs:14-26`.
- Packing Returns calls `/Packing/Returns/PendingFulfillment` (`Mobile.MAUI/Components/Pages/Packing/Returns/ReturnsView.razor.cs:25-43,45,61-81`). API controller: `Api.CoreWebAPI/Controllers/Packing/ReturnsController.cs:14-26`.
- Packing Vendor Return Authorization calls `/Packing/VendorReturnAuthorization/PendingReturn` (`Mobile.MAUI/Components/Pages/Packing/VendorReturnAuthorization/VendorReturnAuthorizationView.razor.cs:26-45,47,63-83`). API controller: `Api.CoreWebAPI/Controllers/Packing/VendorReturnAuthorizationController.cs:15-27`.
- Solution map identifies separate mobile, API, Application, DTO, integration, and test projects (`CLAUDE.md:3-17`). `WMS.slnx` includes `Mobile.MAUI`, `Api.CoreWebAPI`, `Application.UseCases`, and `Integration.NS.Tests` (`WMS.slnx:1-39`).

The mobile pages and API list actions are verified. The corresponding Application handlers for PO, Returns, Packing TO, Packing Returns, and Vendor Return Authorization were located but their implementation details were not verified during research. The `.claude` guides referenced in `CLAUDE.md:19-30` were not found in the inspected repository location. Re-check current files and guidance before implementation.

## 3. Overview and data flow

```mermaid
sequenceDiagram
    actor Operator
    participant Mobile as Mobile selector
    participant API as API/Application
    participant Source as Transaction source
    Operator->>Mobile: Scan or enter transaction identifier
    Mobile->>API: Operation-specific lookup
    API->>Source: Resolve and validate transaction
    Source-->>API: Eligible transaction or no eligible match
    API-->>Mobile: Result or failure
    alt Eligible result
        Mobile-->>Operator: Open existing transaction workflow
    else Failure or ineligible
        Mobile-->>Operator: Show message; allow retry
    end
```

On page entry, the selector renders input controls without invoking its pending-list request. A scan or manual submission invokes the appropriate lookup. The server resolves the identifier for the requested operation and enforces access and eligibility. A valid response provides the existing destination workflow with the transaction context it needs. The detail page continues to load lines and process item-level work as before.

## 4. Components and contracts

### Mobile selector pages

Own scanner/manual input, normalization, submission state, feedback, and successful navigation. They must not search a previously fetched pending list or issue the list request on initial render. Ensure scanner event subscriptions are disposed correctly.

### API and Application lookup

Provide/reuse operation-specific lookup requests for the six transaction types. Each lookup resolves one expected transaction and checks authorization, subsidiary, current status, and operation eligibility on the server. Return only the data required by the established navigation/detail workflow. Exact query, DTO, handler, and source integration paths must be selected after inspecting current operation handlers and conventions; do not assume existing single-record endpoints.

The response must distinguish a valid transaction from a safe failure outcome (not found, wrong type, or not eligible/authorized). Errors must not disclose restricted transaction details. Existing list APIs may remain for other consumers.

### Existing detail workflows

Remain unchanged. They continue to load the selected transaction’s item lines and perform existing item scanning, saving, and status changes.

### Data model

No database/schema change is assumed. Confirm this against the verified source query paths before implementation.

## 5. Correctness properties

- **P1 — No eager list load:** Opening any in-scope selector makes no pending-list request. (Requirements 3.1–3.6)
- **P2 — Server lookup:** Submitted scan/manual input causes an operation-specific lookup; selection does not filter an eagerly fetched client list. (Requirements 3.1–3.6)
- **P3 — Eligible-only navigation:** The app navigates only when the server returns a transaction eligible for that operation and operator. (Requirements 4.1)
- **P4 — Recoverable failure:** Empty, unknown, wrong-type, unauthorized/ineligible, and request-failure cases stay on the selector and permit retry. (Requirements 4.1)
- **P5 — Duplicate protection:** Concurrent or repeated input during a pending lookup does not produce duplicate submissions or navigation. (Requirements 3.1–3.6)
- **P6 — Preserve workflow:** Successful lookup passes the required transaction context to existing downstream screens; detail loading and processing remain unchanged. (Requirements 4.1, 6)
- **P7 — Server authorization:** Guessing or possessing an identifier cannot bypass server authorization or subsidiary restrictions. (Requirements 4.1, 6)

## 6. Error and user-visible behavior

- Display progress while lookup is active and prevent duplicate submission until completion.
- Ignore empty input; normalize scanner input consistently (at minimum trim surrounding whitespace).
- For no match, wrong transaction type, or ineligible transaction, show a clear but non-revealing message and retain the selector.
- For network/server errors, show a recoverable message and permit retry.
- Restore input controls on success/failure as appropriate; successful navigation follows existing routing.

## 7. Validation strategy

The solution contains `Integration.NS.Tests` as a .NET 8 xUnit test project (`Integration.NS.Tests/Integration.NS.Tests.csproj:1-29`). No dedicated MAUI/API test project was identified in the solution search. Inspect available test patterns and handler coverage before selecting exact test locations.

- **Application/API tests:** For each operation, cover eligible transaction, not found, wrong type, ineligible status, unauthorized/subsidiary mismatch, and relevant source failure.
- **Mobile/component validation:** Verify no eager list call; scan and manual submission; pending state; success navigation; invalid outcomes; retry; and duplicate-submission protection. If component test infrastructure is absent, record that limitation and use focused manual validation.
- **Regression:** Verify existing detail screens still receive expected navigation context and preserve item-line loading, item scanning, and saving. Verify excluded workflows and other consumers of list endpoints remain unchanged.
- **Performance:** Compare initial selector request count and time-to-open-transaction before/after; distinguish removed list latency from individual lookup latency.

### Property-based testing assessment

**NOT APPLICABLE** for initial scope: the central properties are operation-specific resolution and authorization plus UI state/navigation. Focused example-based tests provide clearer coverage. Reassess if identifier parsing or shared matching logic becomes complex.

## 8. Accepted consequences and exclusions

Accepted: operators cannot browse pending transactions on these selectors; work discovery requires a valid barcode or manually entered identifier. A browse-pending fallback is excluded.

Excluded: Trip Ticket; Inventory Counting; worksheet creation; Receiving fulfillment sub-selection; item/detail pages; business-rule/status/save changes; and database/schema changes unless later evidence proves one necessary and scope is explicitly revised.

## 9. Verification checklist

- Verify each barcode’s identifier and its uniqueness for the expected operation.
- Verify operation-specific eligibility, authorization, subsidiary, and status rules.
- Verify current destination route parameters and required context.
- Audit all consumers of the six pending-list endpoints before changing/removing any endpoint.
- Identify available test patterns and repository build/test instructions.
- Confirm selectors do not fetch lists on entry and excluded workflows remain unchanged.
