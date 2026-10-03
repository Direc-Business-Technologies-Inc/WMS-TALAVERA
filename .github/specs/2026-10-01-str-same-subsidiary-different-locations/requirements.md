# Allow Same-Subsidiary STRs Between Different Locations

- **Date:** 2026-10-01
- **Status:** Confirmed
- **Mode:** BUGFIX
- **Scope:** Same-subsidiary validation in Stock Transfer Request intercompany transfers and returns
- **Related:** None found

## Introduction

When a subsidiary has multiple locations, users need to create an intercompany Stock Transfer Request (STR), including a return, from one location to another within that same subsidiary. The current form rejects matching source and destination subsidiary IDs before users can select the locations.

## Glossary

- **STR:** Stock Transfer Request.
- **Subsidiary:** The organizational entity selected as the STR source or destination.
- **Source location:** The location from which the STR transfers items.
- **Destination location:** The location receiving the items.
- **Intercompany STR:** An STR category whose `IsInterCompany` flag is true, including intercompany transfers and returns.

## 1. Bug Conditions and Current Behavior

### 1.1 Same-subsidiary selections are rejected

When an STR category is intercompany and the selected source and destination subsidiaries have the same ID, the form displays a “cannot be the same” warning and restores the previous subsidiary selection. This occurs in both subsidiary-selection handlers.

### 1.2 Different locations cannot be used to proceed

Source and destination locations are selected separately, but the subsidiary guard prevents users from selecting the same subsidiary at both ends, even when that subsidiary has multiple locations.

## 2. Acceptance Criteria

### 2.1 Same subsidiary with distinct locations

WHEN a user creates an intercompany transfer or return and selects the same subsidiary at both ends, THEN the form SHALL retain both subsidiary selections without showing the same-subsidiary warning.

### 2.2 Different locations within that subsidiary

WHEN the source and destination subsidiaries are the same and the subsidiary has distinct selectable locations, THEN the form SHALL allow the user to select different source and destination locations.

### 2.3 Same location remains invalid

WHEN a user selects the same location as both source and destination, THEN the form SHALL continue to reject that location pairing using the existing location-level validation.

### 2.4 Both selection orders

WHEN a user selects the destination subsidiary before the source subsidiary, or vice versa, THEN the form SHALL apply the same-subsidiary behavior consistently.

### 2.5 Affected categories

WHEN the STR category is an intercompany transfer or either return category, THEN the same-subsidiary behavior in 2.1 SHALL apply.

### 2.6 Other STR behavior

WHEN the STR category is a regular transfer, THEN its existing subsidiary-selection behavior SHALL remain unchanged.

## 3. Scope

### 3.1 In scope

- Remove or revise the UI-level same-subsidiary blocking for intercompany transfers and returns.
- Permit users to choose distinct locations when the source and destination subsidiaries match.
- Preserve the existing location-level restriction against using the same location at both ends.

### 3.2 Out of scope

- Changing NetSuite’s rules or API contract.
- Changing subsidiary or location eligibility, item availability, or inventory rules.
- Changing regular transfer behavior.
- Refactoring the shared dropdown or unrelated STR form behavior.
- Database schema, DTO, or ViewModel changes unless implementation evidence demonstrates they are required.

## 4. Preservation Requirements

### 4.1 Location validation

The existing prohibition against using the same source and destination location SHALL remain.

### 4.2 Location filtering

Source and destination location lists SHALL continue to be filtered according to their respective selected subsidiaries.

### 4.3 Other form behavior

Existing required-field validation, item handling, vendor behavior, and STR submission flow SHALL remain unchanged.
