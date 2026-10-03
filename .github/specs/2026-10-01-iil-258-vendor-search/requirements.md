# IIL_258: Vendor Search on Return to Supplier

- **Date:** 2026-10-01
- **Status:** Confirmed
- **Mode:** BUGFIX
- **Scope:** Vendor search on the Return to Supplier (RTS) create form
- **Related:** None found

## Introduction

When creating an RTS document, users need to locate a vendor by entering identifying text in the Vendor dropdown. The current dropdown does not reliably filter the vendor list. Search is required to match both vendor reference and vendor name.

## Glossary

- **RTS:** Return to Supplier transaction.
- **Vendor reference:** The vendor's reference number or code.
- **Vendor name:** The vendor's display name.
- **Matching vendor:** A vendor whose reference or name contains the entered search text.

## 1. Bug Conditions and Current Behavior

### 1.1 Search text is not applied

When a user opens the Vendor dropdown on the RTS create form and enters search text, the current dropdown configuration does not provide a filter target. The entered text is therefore not applied to the vendor data query, and the results are not narrowed as expected.

### 1.2 Search identifiers differ

The issue screenshot demonstrates searching by vendor reference code. The selector displays vendor names, and the vendor data contains both reference numbers and names. Search must support both identifiers.

## 2. Acceptance Criteria

### 2.1 Search by vendor reference

WHEN a user enters all or part of a vendor reference in the RTS Vendor search field, THEN the dropdown SHALL display vendors whose reference contains the entered text.

### 2.2 Search by vendor name

WHEN a user enters all or part of a vendor name in the RTS Vendor search field, THEN the dropdown SHALL display vendors whose name contains the entered text.

### 2.3 Current search and paging

WHEN the search text changes, THEN the dropdown SHALL request results matching the current text while retaining its existing server-side paging and virtualization behavior.

### 2.4 No matching vendors

WHEN no vendors match the entered text, THEN the dropdown SHALL display its existing empty-results state.

### 2.5 Vendor selection behavior

WHEN a user selects a vendor from the filtered results, THEN existing vendor selection, required-field validation, and dependent subsidiary behavior SHALL remain unchanged.

## 3. Scope

### 3.1 In scope

- Search the RTS Vendor dropdown by vendor reference and vendor name.
- Preserve server-side filtering, paging, virtualization, and the existing empty-results state.
- Ensure search text is handled as a literal when building the vendor query.

### 3.2 Out of scope

- Changing search behavior in other vendor selectors.
- Redesigning the shared dropdown or changing its API unless implementation evidence shows this is necessary.
- Changing RTS submission, vendor eligibility, or dependent-field business rules.
- Generic SuiteQL query-builder hardening unrelated to this vendor search.
- Database schema, DTO, or ViewModel changes.

## 4. Preservation Requirements

### 4.1

Existing vendor selection and required-field validation SHALL remain unchanged.

### 4.2

Existing server-side paging and virtualization SHALL remain in place.

### 4.3

Selecting or changing a vendor SHALL preserve the existing subsidiary workflow and its dependent form behavior.

### 4.4

Other consumers of the shared dropdown and vendor query path SHALL retain their existing behavior.
