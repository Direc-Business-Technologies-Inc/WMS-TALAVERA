# Allow Same-Subsidiary STRs Between Different Locations

- **Date:** 2026-10-01
- **Status:** Confirmed
- **Mode:** BUGFIX
- **Scope:** Same-subsidiary validation in Stock Transfer Request intercompany transfers and returns
- **Related:** None found
- **Requirements:** [requirements.md](requirements.md)

## Overview

The form currently compares subsidiary IDs and rejects a match before users can select source and destination locations. Revise that form behavior to permit matching subsidiaries. Keep the existing location-level validation as the rule that rejects identical source and destination locations.

## Decision Record

- **Problem:** The two subsidiary selection handlers block equal subsidiary IDs for intercompany STR categories, including returns.
- **Decision:** Remove the subsidiary-only blocking from both selection handlers. Do not add a subsidiary-location-count lookup or condition.
- **Rationale:** The relevant distinction is whether source and destination locations differ. The form already loads each location list against its selected subsidiary and has a separate same-location check.
- **Trade-off:** A user may temporarily select the same subsidiary before choosing locations. Required location fields and the existing same-location validation continue to govern completion.
- **Boundary:** Keep the change within the STR form. Do not alter NetSuite behavior or attempt a broader validation refactor.

## What Already Exists

- The source and destination subsidiary dropdowns are displayed for intercompany categories in [STRForm.razor](../../../Web.BlazorServer/Components/Pages/Transaction/StockTransferRequest/Components/STRForm.razor#L120).
- `OnSubsidiaryChanged` rejects a matching subsidiary ID in [STRForm.razor.cs](../../../Web.BlazorServer/Components/Pages/Transaction/StockTransferRequest/Components/STRForm.razor.cs#L291).
- `OnToSubsidiaryChanged` applies the symmetric rejection in [STRForm.razor.cs](../../../Web.BlazorServer/Components/Pages/Transaction/StockTransferRequest/Components/STRForm.razor.cs#L521).
- Source and destination location providers load locations using their respective selected subsidiary IDs in [STRForm.razor.cs](../../../Web.BlazorServer/Components/Pages/Transaction/StockTransferRequest/Components/STRForm.razor.cs#L147).
- The form has a separate same-location check in [STRForm.razor.cs](../../../Web.BlazorServer/Components/Pages/Transaction/StockTransferRequest/Components/STRForm.razor.cs#L338).
- Intercompany transfers and both return categories have `IsInterCompany = true` in [TransferCategory.cs](../../../Application.DataTransferObjects/Transactions/StockTransferRequest/TransferCategory.cs#L23).

The solution targets .NET 8; the relevant UI is Blazor Server with Radzen, and STR submission uses the existing NetSuite integration. No automated test project was found. The only existing spec, IIL_258, concerns vendor search and does not provide a related STR decision.

## Design Principles and Flow

Keep subsidiary choice independent from location equality. Once the subsidiaries are selected, each location dropdown continues to load locations for its corresponding subsidiary. The existing location validation rejects choosing the same location at both ends.

```mermaid
sequenceDiagram
    actor User
    participant Form as STRForm
    participant Source as Source location provider
    participant Destination as Destination location provider
    participant Validation as Location validation

    User->>Form: Select same subsidiary at both ends
    Form-->>User: Retain both subsidiary selections
    User->>Source: Select source location
    Source-->>User: Locations for source subsidiary
    User->>Destination: Select destination location
    Destination-->>User: Locations for destination subsidiary
    Form->>Validation: Check source and destination locations
    Validation-->>Form: Reject identical locations; allow distinct locations
```

## Verified Technologies and Constraints

- `Web.BlazorServer` targets .NET 8 and contains the STR form and its code-behind.
- The STR form uses Radzen and the repository's `QuickVirtualizedDropdown` abstraction.
- STR data retrieval and submission use the existing handler and NetSuite integration path.
- Blazor components follow the handler-interface boundary; no direct MediatR, repository, or `DbContext` access is required for this UI validation change.
- No domain or database change is supported by the current behavior or acceptance criteria.
- No automated test project was found. Use focused UI verification and the solution build; do not claim automated regression coverage.
- `CLAUDE.md` has conflicting branch timing instructions: create a fix branch before work versus creating/switching after a successful build. Resolve that workflow conflict before implementation.

## Components and Responsibilities

### STR form code-behind

Revise the two subsidiary-change handlers so they no longer reject matching subsidiaries. Preserve their existing dependent-field behavior, including clearing or resetting location selections when a subsidiary changes.

Keep same-location validation in the location-change handlers. Do not introduce asynchronous location-count checks into subsidiary selection.

### Location dropdowns

Keep the existing provider behavior: the source location list follows the source subsidiary, and the destination location list follows the destination subsidiary. No shared dropdown API changes are planned.

### NetSuite submission

Keep the existing submission path. Testing that NetSuite accepts and persists a same-subsidiary, different-location transaction requires an authorized test environment; that external behavior is not established by the current code evidence.

## Correctness Properties

1. Selecting the same subsidiary at both ends of an intercompany STR does not restore the previous selection or show a same-subsidiary warning. _Requirements: 2.1_
2. The same behavior applies whether the source or destination subsidiary is selected first. _Requirements: 2.4_
3. Intercompany transfers and both return categories permit matching subsidiaries. _Requirements: 2.5_
4. Location dropdowns continue to use their respective selected subsidiaries when both subsidiary IDs match. _Requirements: 2.2, 4.2_
5. Selecting identical source and destination locations remains invalid. _Requirements: 2.3, 4.1_
6. Regular transfer subsidiary behavior remains unchanged. _Requirements: 2.6_
7. Required-field validation, item handling, vendor behavior, and existing submission flow remain unchanged. _Requirements: 4.3_

## Error Handling and User-Visible Behavior

Do not show the same-subsidiary warning for intercompany transfers or returns. Retain the existing user-visible validation when source and destination locations are identical. No new notifications are required.

## Validation Strategy

- **Baseline:** When suitable test data and access are available, reproduce the current block in the UI before the fix. If not available, record that limitation rather than claiming a live reproduction.
- **After fix, UI:** For an intercompany transfer and a return, select the same subsidiary at both ends, in both selection orders. Confirm the selections remain and that distinct locations are available.
- **Preservation, UI:** Try the same location at both ends and confirm the existing validation rejects the pair. Verify regular transfer behavior and required-field validation remain unchanged.
- **External integration:** If authorized test NetSuite access is available, submit a controlled transaction using the same subsidiary and different locations, then verify its persisted values. Do not treat a UI-only check as proof of NetSuite acceptance.
- **Build:** Follow the repository's shell probe (`echo ok`) and then run `dotnet build` from the solution root. No automated test result should be claimed unless a suitable test project is established.

## Property-Based Testing

**NOT APPLICABLE.** The behavior is a small deterministic form rule, and the repository has no discovered automated test project. Focused UI checks cover the relevant selection orders, categories, and location-equality preservation without introducing new test infrastructure.

## Accepted Consequences and Out of Scope

- A matching subsidiary selection can exist temporarily while the user chooses locations.
- The fix does not change which locations are valid for a subsidiary, inventory availability, or NetSuite's own business rules.
- Database, DTO, ViewModel, and shared-control changes are not planned.
- No unrelated cleanup of existing location validation is included.

## Verification Checklist

- Same subsidiary retained for intercompany transfer and returns.
- Behavior works regardless of which subsidiary is selected first.
- Each location dropdown shows locations for its selected subsidiary.
- Identical locations remain rejected; distinct locations can be selected.
- Regular transfer and required-field behavior remain unchanged.
- Solution build succeeds.
- NetSuite persistence is verified separately if authorized test access is available.
