# Printed Document Indicator — Tasks

| Metadata | Value |
|---|---|
| Date | 2026-10-29 |
| Status | Confirmed |
| Mode | FEATURE |
| Scope | WMS Web print-status UI for specified transaction pages; UI-only placeholders |
| Related | None found |
| Spec | [design.md](design.md) |

## Goal

Add For Printing / Printed / All UI views and visible print-status/count placeholders to the confirmed WMS Web lists. Default to For Printing only when no status selection is specified. Preserve existing navigation and transaction behaviors; do not integrate with NetSuite or persist print counts in this phase.

## Architecture summary

- Modify existing Blazor page/grid components in `Web.BlazorServer`.
- Keep placeholder values and view selection UI-only.
- Retain existing list providers, handler dispatchers, print actions, permissions, and grid settings.
- Do not alter DTOs, NetSuite queries, custom fields, database schema, or mobile UI.

## Global constraints

- Follow the Web handler/dispatcher boundary; components must not directly call MediatR, repositories, or `DbContext`.
- Preserve existing transaction-tab/query navigation. For Printing is the default only when no status selection is specified.
- Do not imply placeholder values are persisted or NetSuite-backed.
- Do not change print workflows or add print actions.
- Re-check project conventions before implementation; the `.claude/` guide files referenced from `CLAUDE.md` were not present in the working tree during specification work.

## Phase 1 — Verify entry points

### 1. Locate the Packing Labels Web list and print flow

Search the Web project for the Packing Labels page/grid and identify its existing data provider, row actions, and navigation. Record exact component files before changing code. Do not assume the mobile `BoxLabelPrinting.js` is the relevant Web UI. Do not create a print action.

**Completion criteria:** Exact Web page/grid files and existing print flow are documented; any missing list/print action is surfaced for a scope decision.

_Requirements: 3.1, 3.2, 4.1_

### 2. Verify the Trip Ticket print flow

Inspect the Trip Ticket grid and related pages/forms/actions to find whether printing is available elsewhere. Attach status UI to the verified existing list entry point only; do not introduce a print action.

**Completion criteria:** The existing print flow and relevant list entry point are identified, or the absence of one is reported before changing scope.

_Requirements: 3.1, 3.2, 4.1_

**Checkpoint:** Confirm implementation locations and any scope discrepancy for Packing Labels or Trip Tickets before proceeding.

## Phase 2 — Add status views and placeholders

### 3. Add status views to Receiving lists

Update `Web.BlazorServer/Components/Pages/Transaction/Receiving/ReceivingPage.razor` and the relevant existing grid components, including `Receiving/Components/PurchaseOrderGrid.razor` and the verified Stock Transfer Request, Returns, and Receiving Reports list components. Add the views to applicable lists, preserving page-level transaction tabs and existing row actions.

**Completion criteria:** Purchase Orders, Receiving Stock Transfer Request, Returns, and Receiving Reports lists expose the agreed status views and placeholder indicators/counts without changing data-provider contracts.

_Requirements: 1.2–1.4, 2.1–2.2, 3.1–3.2, 4.1_

### 4. Add status views to Inventory Stock Transfer Request lists

Update `Web.BlazorServer/Components/Pages/Transaction/StockTransferRequest/StockTransferRequestPage.razor` and `StockTransferRequest/Components/STRDataGrid.razor` as needed to cover Transfer Orders, Intercompany Transfer Orders, and Returns. Preserve the `Tab` query mapping and URL updates.

**Completion criteria:** All three transaction tabs support the status views and placeholders; existing transaction-tab navigation remains unchanged.

_Requirements: 1.1–1.4, 2.1–2.2, 3.1, 4.1–4.2_

### 5. Add status views to Return to Supplier

Update `Web.BlazorServer/Components/Pages/Transaction/SupplierReturn/SupplierReturnPage.razor` and `SupplierReturn/Components/SupplierReturnsDataGrid.razor`. Preserve status filtering, data loading, print link, and row navigation.

**Completion criteria:** The existing Return to Supplier list exposes status views and placeholders while retaining its current behavior.

_Requirements: 1.2–1.4, 2.1–2.2, 3.1–3.2, 4.1_

### 6. Add status UI to verified Trip Ticket and Packing Labels lists

Use the exact component files established in Tasks 1 and 2. Add status views and placeholders at the existing relevant lists only. Do not add or change print workflows.

**Completion criteria:** Both named areas are either implemented at verified existing lists or any unavailable list/print entry point has been escalated for scope resolution.

_Requirements: 1.2–1.4, 2.1–2.2, 3.1–3.2, 4.1_

### 7. Implement default selection and precedence

For each affected list with status selection, default to For Printing only when none is specified. Preserve supported explicit selection and existing page transaction-tab navigation. Keep transaction tabs separate from status views.

**Completion criteria:** Direct navigation with no status selection opens For Printing; supplied status and existing query-tab selections are retained.

_Requirements: 1.1, 4.2_

### 8. Render UI-only placeholder status and counts

Render visibly temporary placeholder values without adding fields to persistent DTO/domain models or changing server-side data contracts. Do not increment counts when existing print actions are used.

**Completion criteria:** Placeholders are clearly identifiable as temporary UI values, and neither print actions nor view changes persist or mutate a count.

_Requirements: 1.5, 2.1–2.3, 4.3–4.4_

## Phase 3 — Validate and regress

### 9. Verify view selection and placeholder rendering

Use available Web UI/component test infrastructure if present; otherwise note its absence and perform manual checks. Verify default selection, explicit-selection precedence, status-view switching, and placeholder rendering.

**Completion criteria:** Each behavior is verified through available automated checks or documented manual checks.

_Requirements: 1.1–1.5, 2.1–2.2, 4.2_

### 10. Regression-check existing behavior

For each in-scope list, verify data loading, filters, grid settings, permissions, row navigation, and existing print actions. Verify printing does not change placeholder counts.

**Completion criteria:** Existing list and navigation behavior is preserved and print actions do not cause a tracking side effect.

_Requirements: 2.3, 4.1–4.3_

### 11. Review changes and validate the build

Review the final diff for accidental integration/schema changes and unrelated UI changes. Follow the repository build procedure after implementation. This specification task does not authorize committing or pushing.

**Completion criteria:** Relevant build/validation output is captured, and the diff stays within the confirmed UI-only scope.

_Requirements: 4.1–4.4_

## Completion criteria

- Each confirmed list has For Printing, Printed, and All views.
- For Printing is the default only when no status selection is specified.
- Explicit status selection and existing page/query tab selection are preserved.
- Applicable grids show UI-only status/count placeholders that are not represented as persisted values.
- Printing does not update or persist a count.
- Packing Labels and Trip Ticket entry points are verified rather than assumed.
- Existing list, print, permissions, and navigation behavior passes regression checks.
