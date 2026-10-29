# Printed Document Indicator — Requirements

| Metadata | Value |
|---|---|
| Date | 2026-10-29 |
| Status | Confirmed |
| Mode | FEATURE |
| Scope | WMS Web print-status UI for specified transaction pages; UI-only placeholders, with NetSuite integration deferred |
| Related | None found |

## Introduction

Warehouse users need to tell whether a transaction document is awaiting printing or has been printed, and to see a print count. This first delivery adds the UI affordances for those views and indicators. It uses UI-only placeholders until the NetSuite custom fields and their semantics are established with the NetSuite administrator.

## Glossary

- **Print status:** Whether a document is categorized as “For Printing” or “Printed.”
- **Print count:** A displayed number associated with the relevant record/list. Its authoritative counting semantics are not yet defined.
- **Transaction page tab:** An existing grouping on a transaction page, such as Receiving’s Purchase Orders, Stock Transfer Request, or Returns tabs.
- **Status view:** One of the “For Printing,” “Printed,” or “All” views shown in the mockup.
- **UI-only placeholder:** Temporary display state that is not read from or persisted to NetSuite or another backend.

## User stories

- As a warehouse user, I want to see documents awaiting printing so I can identify paperwork that has not yet been printed.
- As a warehouse user, I want to see printed status and a count so I can avoid unnecessary duplicate paperwork.
- As a warehouse user, I want an All view so I can find records regardless of print status.

## Requirements

### 1. Print-status views

**1.1.** WHEN a user opens an affected list with no status-view selection specified, THEN the UI SHALL select **For Printing** by default. WHEN a status-view selection is specified, THEN the UI SHALL preserve that selection.

**1.2.** WHEN a user selects **For Printing**, THEN the UI SHALL display the corresponding status view using UI-only placeholder behavior in this phase.

**1.3.** WHEN a user selects **Printed**, THEN the UI SHALL display the corresponding status view using UI-only placeholder behavior in this phase.

**1.4.** WHEN a user selects **All**, THEN the UI SHALL display the combined status view using UI-only placeholder behavior in this phase.

**1.5.** WHILE NetSuite custom fields are unavailable, THEN the UI SHALL treat status and count as placeholders and SHALL NOT represent them as retrieved or persisted values.

### 2. Status and count indicators

**2.1.** WHEN transaction rows are displayed in an affected list, THEN the UI SHALL provide a visible print-status indicator and placeholder print count consistent with the approved design.

**2.2.** WHEN a user views a record in a status list, THEN the UI SHALL make the status and placeholder count visible in that list.

**2.3.** WHEN a user triggers an existing print action, THEN this phase SHALL NOT persist a count or update NetSuite status.

### 3. Affected pages and record groups

**3.1.** The initial scope SHALL include the following WMS Web pages and lists:

- **Purchasing A/P → Receiving:** Purchase Orders, Stock Transfer Request, Returns, and Receiving Reports tabs.
- **Return to Supplier.**
- **Inventory → Stock Transfer Request:** Transfer Orders (TO), Intercompany Transfer Orders (ITO), and Returns tabs.
- **Delivery → Trip Tickets.**
- **Packing Labels.**

Both Stock Transfer Request locations are in scope.

**3.2.** WHEN a listed page or tab has an existing print action, THEN its print-status UI SHALL apply to the corresponding record list and SHALL NOT change unrelated transaction types.

### 4. Preservation and exclusions

**4.1.** Existing transaction data loading, row actions, print links, permissions, filters, grid settings, and page navigation SHALL continue to work unless explicitly changed by this feature.

**4.2.** Existing tab navigation behavior, including URL/query-tab behavior, SHALL be preserved. A supplied status-view selection SHALL NOT be overridden by the default.

**4.3.** This phase SHALL be limited to WMS Web UI changes. Mobile behavior, NetSuite custom-field creation, integration changes, and automatic status/count persistence are out of scope.

**4.4.** Print-count semantics (including what is counted and whether the count is per transaction or per document type) and authoritative status mapping SHALL remain unresolved and deferred for consultation with the NetSuite administrator. This phase SHALL use UI-only placeholders without implying those semantics are settled.
