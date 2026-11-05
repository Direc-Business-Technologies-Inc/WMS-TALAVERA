---
Date: 2026-10-09
Status: Confirmed
Mode: FEATURE
Scope: Stock Transfer Request — add "Requested By" header field (TO, ITO, Return) sourced from NetSuite employees, persisted to the standard `employee` field, and surfaced as a new list-grid column
Related: `2026-10-06-preferred-vendor-per-line/` (same module and same STR form; per-line Preferred Vendor, unrelated field)
---

# Requirements Document

## Introduction

Stock Transfer Requests currently capture **Prepared By**, which is derived
automatically from the authenticated user's NetSuite employee claim
(`StockTransferRequestHandler.cs:78-81`) and is not user-selectable. There is
no field identifying *which person or department asked for the transfer* when
that person differs from the person preparing the document.

This feature adds a **Requested By** header field to all three Stock Transfer
Request types, sourced from a NetSuite employee dropdown, persisted to the
standard NetSuite `employee` transaction field, and displayed as a new column on
the STR list grid.

## Glossary

- **STR**: Stock Transfer Request. NetSuite records of type `transferorder` and
  `intercompanytransferorder`.
- **STR type**: one of Transfer Order, Intercompany Transfer Order, Returns.
  Returns further subdivide into transfer categories 3 (Returns Good) and 4
  (Returns Bad).
- **Draft**: an STR whose status permits editing — `StatusId` is 2 or 3 **and**
  it has not been submitted for approval. Determined by
  `StockTransferRequestIntegration.cs:207`.
- **Submitted for approval**: `t.custbody_dbti_submitted_for_approval = true`.
- **Requested By**: the NetSuite employee on whose behalf the transfer is
  requested. Stored in the standard NetSuite `employee` transaction field.
- **Prepared By**: the authenticated WMS user's NetSuite employee. Existing,
  auto-generated, not user-selectable.

## Functional Requirements

### 3.1 Capture on creation

The STR create form SHALL display a **Requested By** dropdown, populated from
NetSuite employees, in the header Details area. The field SHALL be independent
of the authenticated user.

### 3.2 Mandatory on creation

Requested By SHALL be required when creating an STR. The create form SHALL NOT
submit while Requested By is unselected.

### 3.3 Editable while draft

On the update form, Requested By SHALL be user-editable **only while the STR is
in Draft**. Once the STR is submitted for approval, the field SHALL render
read-only.

### 3.4 Read-only display

On the view form and in all read-only rendering contexts, Requested By SHALL
display the employee's name, or a placeholder when unset.

### 3.5 NetSuite persistence

The selected Requested By SHALL be included in the NetSuite create and update
payloads as the standard `employee` field, using entity-reference object form
(`{ "id": "<internalId>" }`).

### 3.6 Legacy records with no value

Where an existing STR has no Requested By value, the update form SHALL require
the user to select one before the update can be submitted.

Rationale: the NetSuite field is mandatory, and a `PATCH` that omits the key
leaves it null, which NetSuite rejects for the whole document.

### 3.7 Read path

Retrieving a single STR SHALL return both the Requested By **internal id** and
**display name**. Returning the name alone is explicitly insufficient — this is
the defect the existing `PreparedBy` field exhibits
(`StockTransferRequestIntegration.cs:157`).

### 3.8 List grid column

All three STR list tabs SHALL display a **Requested By** column, filterable and
sortable, consistent with the existing Prepared By column.

### 3.9 Dropdown source

The dropdown SHALL source NetSuite employees through the existing employee
query path (`IUserManagementHandler.GetAllEmployeesAsync` →
`GetAllEmployeesQry`). It SHALL NOT introduce a new employee retrieval
mechanism.

**Source constraint (verified):** that path filters to
`custentity_dbti_other_roles Contains "WMS"`
(`Integration.NS/Implementations/Others/EmployeeIntegration.cs:27-32`), so only
WMS-tagged NetSuite employees are selectable.

## Out of Scope

- Packing STR (`Packing/STR/*`) — confirmed excluded.
- Inventory Transfer Request — a separate module, not covered.
- Any NetSuite-side change: field creation, customizations, workflows,
  SuiteScripts.
- Changing **Prepared By** behavior.
- Changing the grid's `IsEditable` definition
  (`StockTransferRequestDataGridNSDTO.cs:28`).
- Backfilling existing NetSuite records.
- Subsidiary-scoping the employee dropdown. Deferred — not requested.
- Adjacent refactors noted in the design document's accepted consequences.

## Preservation Requirements

### 5.1

Existing STR behavior SHALL NOT change for any field other than Requested By.
In particular, Prepared By shall continue to be claim-derived and shall continue
to be omitted from update payloads.

### 5.2

The update path SHALL NOT begin sending `custbody_dbti_prepared_by`. Prepared By
currently round-trips as name-only with no id; changing that is a separate
concern.

### 5.3

Create and update SHALL continue to swallow the "Empty response from NetSuite
API" exception (`StockTransferRequestIntegration.cs:294, 312, 330`).

### 5.4

Payload serialization SHALL continue to use `JsonNamingPolicy.CamelCase` with
`DefaultIgnoreCondition = WhenWritingNull`
(`StockTransferRequestIntegration.cs:349-354`).

### 5.5

Grid results remain cached by `CachedStockTransferRequestIntegration.cs`. Stale
cache behavior is a deployment concern, not a behavior change.

## Acceptance Criteria

- **AC-1** — WHEN a user opens any of the three STR create forms, THEN a
  Requested By dropdown SHALL be visible, populated with NetSuite employees, and
  searchable by name.
- **AC-2** — WHEN a user submits the create form with Requested By unselected,
  THEN submission SHALL be blocked with a validation message, and no NetSuite
  request SHALL be issued.
- **AC-3** — WHEN a user creates an STR with Requested By selected, THEN the
  resulting NetSuite record SHALL have the standard `employee` field set to that
  employee's internal id.
- **AC-4** — WHEN a user opens a Draft STR for editing, THEN Requested By SHALL
  be editable and SHALL show the currently stored employee.
- **AC-5** — WHEN a user opens a submitted-for-approval STR, THEN Requested By
  SHALL render read-only with the stored employee's name.
- **AC-6** — WHEN a user edits a Draft STR and changes Requested By, THEN the
  NetSuite `employee` field SHALL reflect the new employee after save.
- **AC-7** — WHEN a user opens a legacy STR that has no Requested By and attempts
  to save, THEN submission SHALL be blocked until a Requested By is selected.
- **AC-8** — WHEN a user views any of the three STR list tabs, THEN a Requested
  By column SHALL be present and populated for records that have a value.
- **AC-9** — WHEN a user filters or sorts the STR list by Requested By, THEN the
  result SHALL reflect the filter or sort.
- **AC-10** — WHEN an existing STR is edited without touching Requested By, THEN
  the stored Requested By SHALL remain unchanged.
- **AC-11** — WHEN a user creates an STR, THEN the Prepared By value SHALL behave
  exactly as it does today.

## Open Item

- **O-1** — Confirm the intended requesters are tagged
  `custentity_dbti_other_roles = "WMS"` in NetSuite (Requirement 3.9 source
  constraint). If requesters are not tagged, the dropdown source must change and
  this specification is incomplete.