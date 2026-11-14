# Bulk Fulfillment Submission — Requirements

- **Date:** 2026-11-09
- **Status:** Confirmed
- **Mode:** FEATURE
- **Scope:** Web Supplier Return bulk selection and submission to the NetSuite Bulk Fulfillment custom record.
- **Related:** None found for this specific submission feature.
- **Design:** [design.md](design.md)
- **Tasks:** [tasks.md](tasks.md)

The user confirmed requirements, design, and tasks in the conversation before authorizing these files. Earlier conversational drafts used 2026-11-06; this package uses the actual writing date. The confirmed design clarifies that at least two unique RTS records are required.

## 1. Introduction

The existing Bulk Fulfill UI collects selected RTS records and opens a confirmation dialog but does not submit a request. This feature creates one NetSuite Bulk Fulfillment record for a confirmed selection. Downstream processing, planned-quantity interpretation, inventory allocation, and Item Fulfillment creation remain NetSuite-owned.

## 2. Glossary

- **RTS:** Return to Supplier, identified by its NetSuite transaction internal ID.
- **IF:** Item Fulfillment; not created by this WMS feature.
- **BF record:** One `customrecord_bulk_fulfillment` record grouping selected RTS references.
- **Submitted:** Initial BF status with internal ID `1` in the account shown by the user.
- **Submission success:** Confirmed creation of the BF record, not completed fulfillment.
- **Uncertain outcome:** Creation may have occurred but WMS cannot confirm acceptance.

## 3. Verified Context and Contract

The repository review located existing selection, confirmation, presentation dispatch, application commands, and NetSuite integration. The bulk confirmation has no executable submission behavior. Existing individual RTS fulfillment is separate.

The user's NetSuite screenshots and colleague's instructions establish:

| Field | Script ID | Submission value |
| --- | --- | --- |
| Record type | `customrecord_bulk_fulfillment` | One new record per submission |
| BF date | `custrecord_bf_date` | Server-resolved business date |
| BF Status | `custrecord_bf_status` | Submitted, internal ID `1` |
| BF Return to Supplier IDs | `custrecord_bf_return_to_supplier_ids` | Unique selected RTS internal-ID references |
| BF Error Message | `custrecord_bf_error_message` | Omitted |

The displayed custom list is `customlist_bulk_fulfillment_status`: Submitted `1`, Done `2`, Failed `3`, Processing `4`. WMS sets only Submitted in this scope. These account values must be checked in the target environment.

RTS IDs are logically an array; the actual NetSuite multi-select HTTP representation remains to be verified. A logical ID array is not evidence that a raw JSON integer array is accepted.

## 4. User Story

As an authorized warehouse user, I want to select multiple RTS records and submit them for bulk fulfillment in NetSuite so I do not have to initiate each request individually.

## 5. Functional Requirements

### 5.1 Selection

- **5.1.1** WHEN fewer than two unique RTS records are selected, THEN WMS SHALL disable Bulk Fulfill. This incorporates the confirmed design's preservation of the existing minimum-two rule.
- **5.1.2** WHEN records are selected, THEN WMS SHALL identify them by NetSuite internal ID, not document number or row position.
- **5.1.3** WHEN preparing submission, THEN WMS SHALL include each selected RTS ID only once.
- **5.1.4** WHEN the grid reloads or filters change, THEN WMS SHALL preserve existing selection-clearing behavior.

Cross-page selection and select-all-matching-results are not introduced.

### 5.2 Confirmation

- **5.2.1** WHEN confirmation opens, THEN WMS SHALL display selected RTS document references and their total count.
- **5.2.2** WHEN confirmation is displayed, THEN WMS SHALL explain that this submits a request, not confirms completed fulfillment.
- **5.2.3** WHEN the user cancels, THEN WMS SHALL create no BF record.
- **5.2.4** WHEN the user confirms, THEN WMS SHALL submit the selection shown in that confirmation. Dialog search SHALL change visibility only, not the submitted selection.

Suggested copy: “Submit these 5 RTS records for bulk fulfillment? This creates a request in NetSuite. Fulfillment is processed separately.” Actions: **Submit Request** and **Cancel**.

### 5.3 Authorization and Input Validation

- **5.3.1** WHEN submission is requested, THEN the authorized server-side presentation entry point SHALL enforce the applicable permission and access scope.
- **5.3.2** WHEN validating selection, THEN WMS SHALL reject nonpositive IDs or fewer than two unique IDs without creating a BF record.
- **5.3.3** WHEN a selected ID does not identify an accessible RTS, THEN WMS SHALL reject the submission without creating a BF record.
- **5.3.4** WHEN validation fails, THEN WMS SHALL explain the issue and SHALL NOT silently omit records from the confirmed selection.

WMS does not validate inventory availability, allocate inventory, or calculate IF quantities. Additional operational eligibility rules must be explicitly agreed rather than inferred.

### 5.4 BF Record Creation

- **5.4.1** WHEN a valid selection is confirmed, THEN WMS SHALL create one BF record, not one record per RTS.
- **5.4.2** WHEN creating the record, THEN WMS SHALL populate the business date, Submitted status `1`, and unique selected RTS references using the field mapping in section 3.
- **5.4.3** WHEN supplying date and status, THEN WMS SHALL resolve them server-side rather than trust arbitrary browser values.
- **5.4.4** WHEN creating the record, THEN WMS SHALL leave `custrecord_bf_error_message` unset.
- **5.4.5** WHEN NetSuite confirms creation, THEN WMS SHALL obtain the BF internal ID. The design specifies truthful exception handling if acceptance is confirmed but the reference cannot be recovered.
- **5.4.6** WHEN submitting, THEN WMS SHALL NOT call IF creation operations or include quantity/allocation payloads.

The business timezone remains to be confirmed; unqualified server-local dates are not an accepted default.

### 5.5 Busy State and Results

- **5.5.1** WHEN submission is in progress, THEN WMS SHALL show a busy state and prevent repeated confirmation clicks.
- **5.5.2** WHEN creation is confirmed, THEN WMS SHALL display submission success including the BF reference when available. Confirmed acceptance with no recoverable reference SHALL explicitly state that limitation and SHALL NOT be treated as rejection.
- **5.5.3** WHEN showing success, THEN WMS SHALL say “Bulk fulfillment request submitted,” not “RTS fulfilled.”
- **5.5.4** WHEN submission succeeds, THEN WMS SHALL close confirmation and clear the submitted selection.
- **5.5.5** WHEN submission is definitively rejected, THEN WMS SHALL preserve selection, restore controls, and show an actionable error.

### 5.6 Uncertain Outcomes and Duplicate Submission

- **5.6.1** WHEN a timeout or connection failure leaves creation uncertain, THEN WMS SHALL explain that the request may have been created.
- **5.6.2** WHEN creation is uncertain, THEN WMS SHALL NOT blindly retry creation automatically.
- **5.6.3** WHEN creation is uncertain, THEN WMS SHALL direct the user to verify NetSuite before resubmitting.

The existing write-safe 429 retry policy is preserved. Button guards do not guarantee cross-user/cross-session deduplication or exactly-once creation. NetSuite duplicate handling remains an external contract to confirm.

## 6. Preservation Requirements

- **6.1** WHEN this feature is introduced, THEN individual RTS scanning, packing, and fulfillment SHALL remain unchanged.
- **6.2** WHEN the grid reloads, THEN existing filtering, paging, and selection semantics SHALL remain unchanged.
- **6.3** WHEN adding submission, THEN component → feature handler → application → integration boundaries SHALL be preserved.
- **6.4** WHEN reporting/logging failures, THEN credentials, tokens, and session metadata SHALL be excluded.

## 7. Scope

In scope: existing selection, confirmation, busy state, BF creation, date/status/reference mapping, receipt, rejection and uncertainty feedback.

Out of scope: IF creation, planned-quantity calculations, inventory allocation, downstream BF status updates, fulfillment retries, monitoring/history dashboard, attachments, Mobile changes, new queues/workers, local BF schema, and unrelated architectural cleanup.

## 8. Validation Expectations

Verify invalid/unauthorized selections create nothing; payload contains the exact unique selected references, business date, and status `1`; cancellation creates nothing; repeated clicks cannot start concurrent requests; confirmed submission never claims completed fulfillment; rejection and uncertainty differ; no IF operation is invoked; and an authorized sandbox check establishes serialization, permissions, and receipt behavior. These are required checks, not claims of completed testing.
