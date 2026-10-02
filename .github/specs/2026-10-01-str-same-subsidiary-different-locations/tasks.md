# Allow Same-Subsidiary STRs Between Different Locations

- **Date:** 2026-10-01
- **Status:** Confirmed
- **Mode:** BUGFIX
- **Scope:** Same-subsidiary validation in Stock Transfer Request intercompany transfers and returns
- **Related:** None found
- **Spec:** [design.md](design.md)

## Goal

Allow intercompany transfers and returns to use the same subsidiary at both ends, so users can select distinct source and destination locations within that subsidiary. Keep the existing rejection of identical locations and preserve other STR behavior.

## Architecture Summary

The change is limited to the subsidiary-selection handlers in `Web.BlazorServer`'s STR form. Location providers continue filtering locations by their respective subsidiary selections, and existing location-level validation continues to reject identical source and destination locations. Submission continues through the existing handler and NetSuite integration path.

## Verified Technical Context

- The subsidiary guards are in [STRForm.razor.cs](../../../Web.BlazorServer/Components/Pages/Transaction/StockTransferRequest/Components/STRForm.razor.cs#L291) and [STRForm.razor.cs](../../../Web.BlazorServer/Components/Pages/Transaction/StockTransferRequest/Components/STRForm.razor.cs#L521).
- The now-unneeded `SameSubsidiary` helper is in [STRForm.razor.cs](../../../Web.BlazorServer/Components/Pages/Transaction/StockTransferRequest/Components/STRForm.razor.cs#L589).
- Source and destination location providers and the same-location validation are in the same form code-behind.
- The relevant projects target .NET 8.
- No automated test project was found. Verification therefore relies on focused UI checks and the solution build unless test infrastructure is introduced separately.
- `CLAUDE.md` contains conflicting branch timing instructions: it says to create a branch before implementation, but also to switch branches after a successful build. Resolve that workflow conflict before implementation; this task plan does not create branches, commits, or pushes.

## Global Constraints

- Keep the source change in the STR form code-behind; do not introduce a new application, domain, database, or integration layer.
- Remove the subsidiary-only blocks in both selection handlers and remove the now-unused helper.
- Preserve location-level validation, location filtering, required-field validation, item behavior, vendor behavior, and submission flow.
- Do not change regular transfer behavior, shared dropdown behavior, DTOs, ViewModels, schema, or NetSuite rules.
- Do not claim automated regression coverage; no test project was found.

## Tasks

### Phase 1: Baseline Reproduction

1. Before editing, attempt to reproduce the block in the UI for an intercompany transfer and a return. Select the same subsidiary at both ends, including trying each selection order. Record whether the warning and selection rollback occur. _Requirements: 1.1, 2.1, 2.4, 2.5_
   - **Files:** No source edits; exercise the STR create forms.
   - **Completion criteria:** Record whether the reported behavior is observed. If suitable UI access or test data is unavailable, record that limitation.

### Phase 2: Revise Subsidiary Validation

2. In [STRForm.razor.cs](../../../Web.BlazorServer/Components/Pages/Transaction/StockTransferRequest/Components/STRForm.razor.cs#L291), remove the same-subsidiary rejection from `OnSubsidiaryChanged`. Preserve the handler's remaining dependent-field and location-reset behavior. _Requirements: 2.1, 2.4, 2.5_
   - **Current:** Matching subsidiary IDs display a warning and restore the previous source subsidiary.
   - **Target:** A matching subsidiary selection is retained.
   - **Completion criteria:** Selecting a source subsidiary matching the selected destination subsidiary no longer triggers the warning or rollback.

3. In [STRForm.razor.cs](../../../Web.BlazorServer/Components/Pages/Transaction/StockTransferRequest/Components/STRForm.razor.cs#L521), remove the corresponding rejection from `OnToSubsidiaryChanged`. Preserve existing destination-location and vendor reset behavior. Remove `SameSubsidiary` if it has no remaining usages. _Requirements: 2.1, 2.4, 2.5_
   - **Current:** Matching subsidiary IDs display a warning and restore the previous destination subsidiary.
   - **Target:** A matching subsidiary selection is retained.
   - **Completion criteria:** Selecting a destination subsidiary matching the selected source subsidiary no longer triggers the warning or rollback; no unused helper remains.

### Phase 3: Verify the Fix and Preserved Behavior

4. In the UI, verify an intercompany transfer and a return. For each flow, select a subsidiary with at least two available locations at both ends, test both subsidiary-selection orders, then select different source and destination locations. _Requirements: 2.1, 2.2, 2.4, 2.5, 4.2_
   - **Files:** No additional changes expected.
   - **Completion criteria:** Both subsidiary selections remain selected, and distinct locations for that subsidiary are available and selectable. If the UI exposes both return categories, verify both; otherwise record which category was exercised.

5. Verify preservation behavior in the UI: attempt to use the same location at both ends, exercise regular transfer, and check required-field validation and existing form behavior. _Requirements: 2.3, 2.6, 4.1, 4.3_
   - **Files:** No additional changes expected unless a defect in the touched behavior is found.
   - **Completion criteria:** Identical locations remain invalid; regular transfer, required fields, item handling, vendor behavior, and submission flow remain unchanged.

6. From the solution root, run the repository-prescribed shell probe (`echo ok`), then `dotnet build`. _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 4.1, 4.2, 4.3_
   - **Completion criteria:** The shell probe succeeds and the solution build succeeds. If the probe fails as described in `CLAUDE.md`, stop and report that limitation. No automated test result is claimed.

## Optional Work

None. Adding a test project, modifying NetSuite behavior, or refactoring location validation is outside this issue's scope.
