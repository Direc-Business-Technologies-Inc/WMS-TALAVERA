# Tasks — Receiving and Packing Print Workflows

**Date:** 2026-10-30  
**Status:** Confirmed  
**Mode:** FEATURE  
**Scope:** Implement confirmed Receiving and Packing print workflows, remove standalone report/label tabs, and investigate and correct Printed counts across pages using print-status views.  
**Related:** [Design](design.md)

## Goal

Enable users to filter print status and print individual records from every transaction tab in Receiving and Packing. Remove standalone Receiving Reports and Packing Labels tabs without losing their workflows. Ensure Printed counts reflect the verified NetSuite print-count source.

## Architecture summary and verified technical context

Follow Blazor component → feature handler → MediatR/Application → integration. Reuse existing print-status and print-link patterns where appropriate. Verify NetSuite internal IDs and printout mappings before building row links. Do not increment locally or mark printed on click unless the authoritative source and successful-print signal are verified.

The solution is organized in `WMS.slnx`; the available test project is `Integration.NS.Tests`. The solution lists no dedicated Blazor UI test project. Apply `.claude/blazor_conventions.md`, `.claude/ui_abstractions.md`, `.claude/database_patterns.md`, and `.claude/application_layer.md`. Preserve the documented handler boundary; no database schema change is assumed.

## Global constraints

- Preserve transaction tabs, navigation, filters, authorization, and detail pages.
- Confirm record IDs through the query → DTO → VM flow; do not trust an unselected ID property.
- Preserve existing print URLs/templates unless validation proves a change is needed.
- Do not assume all document types share one print URL or update mechanism.
- Do not count a failed print or merely opened link as printed.
- Do not include Trip Ticket count behavior until its source field is verified.
- Validate external NetSuite behavior in a test environment; local builds cannot confirm external script updates.

## Phase 1 — Inventory and verify print support

### 1.1 Map each tab to its printout and record identity

Review Receiving:
- `Web.BlazorServer/Components/Pages/Transaction/Receiving/ReceivingPage.razor:22-43`
- `Web.BlazorServer/Components/Pages/Transaction/Receiving/Components/PurchaseOrderGrid.razor`
- `Web.BlazorServer/Components/Pages/Transaction/Receiving/Components/TransferOrderGrid.razor`
- `Web.BlazorServer/Components/Pages/Transaction/Receiving/Components/ReturnsGrid.razor`
- `Web.BlazorServer/Components/Pages/Transaction/StockTransferRequest/Components/STRDataGrid.razor`

Review Packing:
- `Web.BlazorServer/Components/Pages/Transaction/Packing/PackingPage.razor:22-43`
- `Web.BlazorServer/Components/Pages/Transaction/Packing/STR/Component/STRPackingDataGrid.razor`
- `Web.BlazorServer/Components/Pages/Transaction/Packing/Returns/Component/ReturnsPackingDataGrid.razor`
- `Web.BlazorServer/Components/Pages/Transaction/Packing/VendorReturnAuthorization/Component/VendorReturnAuthorizationPackingDataGrid.razor`

For each tab, record row type, internal ID availability, print action or detail-page action, verified scriptlet, and any missing mapping or support.

**Completion criteria:** Every transaction tab has a documented printout/ID mapping; missing fields or external support are explicit investigation findings rather than assumptions.  
_Requirements: 1.3, 1.4, 3.1, 5.1_

### 1.2 Trace Printed count updates end to end

Inspect `Web.BlazorServer/Components/Custom/PrintStatusFilter.cs:13-27` and all `PrintStatusView`/`PrintStatusFilter` call sites. Verify which queries apply the print-count field and which NetSuite script/service updates it. Compare the field before/after a controlled print for each supported scriptlet family.

**Completion criteria:** Evidence identifies whether the issue is global or printout-specific; update source and success signal are documented; check failed/aborted printing if supported; no fix is based solely on link click.  
_Requirements: 4.1, 4.2, 4.3, 5.1_

## Phase 2 — Complete row-level print actions

### 2.1 Add or complete Receiving row print actions

Add record-specific print links to Purchase Orders, Receiving-page Stock Transfer Requests, and Returns. Preserve the existing STR transaction print action. Candidate files:
- `Receiving/Components/PurchaseOrderGrid.razor:65-79`
- `Receiving/Components/TransferOrderGrid.razor:67-75`
- `Receiving/Components/ReturnsGrid.razor:71-79`
- `StockTransferRequest/Components/STRDataGrid.razor:96` (existing action)

Add missing internal IDs through the appropriate VM/DTO/integration query only when Phase 1 confirms they are needed. Use the verified document-specific printout and record ID.

**Completion criteria:** Each Receiving transaction tab has a row-level action for its corresponding document; it opens the correct record's printout; invalid IDs do not create malformed links; existing STR printing remains functional.  
_Requirements: 1.3, 1.4, 5.1_

### 2.2 Add or complete Packing row print actions

Add per-row print actions to:
- `Packing/STR/Component/STRPackingDataGrid.razor:47-55`
- `Packing/Returns/Component/ReturnsPackingDataGrid.razor:47-55`
- `Packing/VendorReturnAuthorization/Component/VendorReturnAuthorizationPackingDataGrid.razor:39-47`

Preserve existing Returns-to-Supplier action in `SupplierReturn/Components/SupplierReturnsDataGrid.razor:16-20`. Validate reuse of detail print mappings in `Packing/STR/Component/STRPackingDetails.razor.cs:29,85`, `Packing/Returns/Component/ReturnsPackingDetails.razor.cs:29,85`, and `Packing/VendorReturnAuthorization/Component/VendorReturnAuthorizationPackingDetails.razor.cs:30,86`.

**Completion criteria:** Each Packing transaction tab has a record-specific action using the correct printout/ID; existing Returns-to-Supplier printing remains functional; missing IDs are added through existing integration/DTO/VM boundaries.  
_Requirements: 1.3, 1.4, 3.1, 5.1_

## Phase 3 — Print-status views and page structure

### 3.1 Add print-status views to all Packing tabs

Integrate shared `PrintStatusView` and filter/count/reload behavior in:
- `Packing/STR/Component/STRPackingDataGrid.razor` and `.razor.cs`
- `Packing/Returns/Component/ReturnsPackingDataGrid.razor` and `.razor.cs`
- `Packing/VendorReturnAuthorization/Component/VendorReturnAuthorizationPackingDataGrid.razor` and `.razor.cs`

Ensure counts and filters stay scoped to their transaction category and applicable date range.

**Completion criteria:** All three Packing tabs show For Printing, Printed, All; each filters only its tab's records; counts use the matching query and active filters; reload/count refresh works.  
_Requirements: 3.1, 4.2, 5.1_

### 3.2 Remove standalone printout tabs

Update:
- `Receiving/ReceivingPage.razor:31-42`
- `Receiving/ReceivingPage.razor.cs:19-24,29-50,53-93`
- `Packing/PackingPage.razor:31-42`
- `Packing/PackingPage.razor.cs:18-24,29-54,56-108`

Remove the standalone tabs and tab-only state/handlers after moving their workflows. Re-map tab indexes/query-string values to preserve selection and navigation.

**Completion criteria:** Standalone tabs are gone; remaining tab query/navigation works; applicable workflows are available from transaction tabs.  
_Requirements: 2.1, 5.1_

## Phase 4 — Correct Printed count behavior

### 4.1 Implement the verified count-update fix

Use Phase 1 evidence to fix the smallest verified source of truth. If a NetSuite scriptlet owns the update, coordinate/implement the supported external change; if WMS owns it, update through established integration boundaries. Do not increment solely on link invocation.

Relevant WMS files:
- `Web.BlazorServer/Components/Custom/PrintStatusFilter.cs:13-27,50-58`
- `Web.BlazorServer/Components/Custom/PrintStatusView.razor:33-49`

**Completion criteria:** Successful printing updates the authoritative count; failed/aborted printing does not create false status; Printed view reflects the source after its supported refresh interval; repeated printing follows verified count semantics without double increment.  
_Requirements: 4.1, 4.2, 4.3_

### 4.2 Verify refresh behavior across pages

Review all `PrintStatusView` call sites. Ensure a successful count update becomes visible without unrelated navigation or restart. If NetSuite propagation is asynchronous, use a bounded refresh strategy consistent with existing UI patterns.

**Completion criteria:** Grid and count reflect the updated source within agreed refresh behavior; active category/date filters remain applied; count-query failures are observable and not represented as successful zero.  
_Requirements: 4.2, 5.1_

## Phase 5 — Verification

### 5.1 Add and run focused tests

- Add/update tests in `Integration.NS.Tests` for query ID selection, DTO mapping, and testable integration-side count behavior.
- Test URL construction and invalid-ID handling for changed/new print builders where the existing test conventions support it.
- Add UI automation only if an established test project/framework is found; otherwise record the manual validation requirement.

**Completion criteria:** Relevant ID/mapping and count-filter paths are tested; results and unavailable coverage are reported accurately.  
_Requirements: 1.3, 1.4, 4.1, 4.2, 4.3_

### 5.2 Perform UI and end-to-end regression checks

- Check row print action presence/behavior on every Receiving and Packing tab.
- Check all Packing status views and existing Receiving status views.
- Check standalone tabs are absent and tab/query navigation works.
- In a NetSuite test environment, print records across each supported scriptlet family and verify source field and WMS Printed count.

**Completion criteria:** Every action is record-specific; status views show expected category-scoped records; count behavior is verified for successful and, where possible, unsuccessful print attempts; existing details, filters, and navigation continue to work.  
_Requirements: 1.1, 1.2, 1.3, 2.1, 3.1, 4.1, 4.2, 4.3, 5.1_

## Build and validation

- Use commands supported by `.claude/building_the_project.md` and actual projects in `WMS.slnx`.
- `Integration.NS.Tests` is the solution's available test project; no Blazor UI test project is listed.
- A successful build cannot verify external NetSuite scriptlet behavior; end-to-end NetSuite validation remains necessary.
