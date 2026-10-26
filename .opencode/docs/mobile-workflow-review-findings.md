# Mobile Workflow Review Findings

**Date:** 2026-10-24  
**Status:** Code review complete; behavior-dependent items need workflow confirmation  
**Scope:** Mobile MAUI packing, receiving, returns, and related scan state handling  
**Review type:** Static source review; no device/runtime tests performed

## Executive Summary

The review found several areas to verify before treating the mobile workflows as robust under unusual scan sequences. The most actionable code-level issue is that receiving PO and TO primary scan handlers filter the bad-item row using the good-line selection. The effect depends on how the UI selection is used, so a regression case with repeated SKUs and selected lines is needed before classifying it as a confirmed operational defect.

The review also identified possible weight/quantity divergence during move and negate operations, a returns-vs-PO difference in weight prompting, and an `async void` scan-event pattern that may permit overlapping scans. These are risks or policy questions, not proven inventory corruption. Earlier claims that nullable bad-line dereferences were reachable and that component disposal generally suppresses unsaved-work prompts were rechecked and withdrawn as stated; see **Findings withdrawn or narrowed**.

## Evidence and Findings

### F1. Receiving bad-line lookup uses the good-line selection

**Evidence:**

- `Mobile.MAUI/Components/Pages/Receiving/PurchaseOrder/PurchaseOrderItemView.razor.cs:480-488`
- `Mobile.MAUI/Components/Pages/Receiving/TransferOrder/TransferOrderItemView.razor.cs:275-286`

In the primary scan handlers, the bad row is selected using `GoodSelectedLine` as the line-sequence constraint. The receiving TO move/negate handlers use `BadSelectedLine` when the bad tab is active, indicating that the UI has a distinct bad-line selection concept.

**Assessment:** Confirmed code inconsistency; practical impact is not yet confirmed. Depending on the UI interaction, a selected good line could constrain or redirect the corresponding bad-line match. Verify selection semantics in the `.razor` markup and test repeated SKUs across multiple order lines before changing behavior.

**Recommended verification:** Select a bad line for an SKU that appears on two order lines and scan it in bad mode. Confirm the quantity increments on the intended line. Repeat with a good line selected and with no line selected.

### F2. Move/negate weight handling can diverge from quantity handling

**Evidence:**

- `Mobile.MAUI/Components/Pages/Receiving/PurchaseOrder/PurchaseOrderItemView.razor.cs:851-895, 1051-1092`
- `Mobile.MAUI/Components/Pages/Receiving/TransferOrder/TransferOrderItemView.razor.cs:530-595, 673-735`
- `Mobile.MAUI/Components/Pages/Receiving/Returns/ReturnsItemView.razor.cs:302-323`

Move and negate operations obtain a weight from the operator and adjust an aggregate `ScannedWeight` by that entered value while quantity is adjusted by the barcode's UoM conversion. The source code does not show a per-scan weight allocation that would prove the entered value equals the weight associated with the quantity being moved or removed.

**Assessment:** Potential data-integrity issue if entered weight differs from the quantity's recorded weight. This depends on the intended business rule: re-weighing the moved quantity may be correct, but the aggregate accounting rule should be explicit. In returns, negation similarly prompts for a new weight and subtracts it from the aggregate.

**Recommended next step:** Confirm whether move/negate should reuse recorded weight or re-weigh. If re-weighing is expected, validate the new weight against business constraints and ensure resulting aggregates cannot become negative or inconsistent.

### F3. Returns always prompts for weight

**Evidence:** `Mobile.MAUI/Components/Pages/Receiving/Returns/ReturnsItemView.razor.cs:239-255`

The returns receipt scan path always calls `GetWeightAsync`. PO receiving offers a with-weight/without-weight mode (`PurchaseOrderItemView.razor.cs:59-60, 540-605`).

**Assessment:** Confirmed workflow difference; whether it is a defect depends on whether return receipts always require measured weight. It can block scanning when weight is unavailable or not required.

**Recommended next step:** Confirm the returns weight policy with operations, then either preserve the mandatory prompt as an explicit rule or align returns with the PO receive option.

### F4. Scan event handlers use `async void`

**Evidence:**

- `Mobile.MAUI/Components/Pages/Receiving/PurchaseOrder/PurchaseOrderItemView.razor.cs:432`
- `Mobile.MAUI/Components/Pages/Receiving/TransferOrder/TransferOrderItemView.razor.cs:232`
- Similar handlers exist across mobile packing and receiving detail pages.

The event handlers await barcode-resolution calls and dialogs. An `async void` event handler cannot be awaited by its publisher, so the source does not establish that successive scan events are serialized.

**Assessment:** Concurrency/race risk, not a confirmed duplicate-scan bug. If the broadcast service dispatches another scan while a first handler is awaiting, in-memory quantities may be updated concurrently or scans may interleave around a weight dialog.

**Recommended verification:** Inspect `BroadcastService` dispatch behavior and exercise fast consecutive scans on a device. If calls can overlap, add a single-flight guard or queue scan events; avoid dropping legitimate scans silently.

### F5. Packing TO quantity-cap guard is redundant at exact completion

**Evidence:** `Mobile.MAUI/Components/Pages/Packing/TransferOrder/TransferOrderItemView.razor.cs:792-813`

The handler rejects when existing good+bad quantity is already `>= PackableQuantity`, then checks whether the next barcode quantity exceeds the remaining quantity.

**Assessment:** The first check prevents any further scan at the cap; the second check already rejects a scan that would exceed the cap. This is not necessarily incorrect—the intent may be to block any scan after completion—but that policy is not documented by this code. Exact-limit and over-limit behavior should be confirmed with operations.

### F6. Dirty state is cleared on disposal

**Evidence:**

- `Mobile.MAUI/Components/Pages/Receiving/PurchaseOrder/PurchaseOrderItemView.razor.cs:1205-1210`
- `Mobile.MAUI/Components/Pages/Receiving/Returns/ReturnsItemView.razor.cs:418-421`
- `Mobile.MAUI/Components/Pages/Packing/TransferOrder/TransferOrderItemView.razor.cs:1363-1368`
- `Mobile.MAUI/Services/UnsaveChangesHandlerService.cs:9-19`
- `Mobile.MAUI/Components/Layout/MainLayout/MainLayout.razor.cs:42-50`

Detail components clear the singleton unsaved-changes flag during disposal. `MainLayout` uses that flag to distinguish session expiration with unsaved scans from a clean session expiration.

**Assessment:** A narrow timing risk is possible if the component is disposed with unsaved in-memory scans and session expiry is handled afterward: the expired-session message could omit the warning that scans were not saved. This review did not establish that normal navigation prompts are bypassed; the `Confirmable` flow needs separate tracing. Therefore, the earlier broad claim about ordinary navigation warnings is withdrawn.

**Recommended verification:** Test session expiry after leaving a dirty detail page and inspect `Mobile.MAUI/Components/Reusables/Confirmable.razor.cs` navigation behavior. Keep the dirty flag set until successful save or explicit discard if disposal can precede those outcomes.

## Findings Withdrawn or Narrowed

### Nullable bad-line dereference

The receiving TO and packing TO code dereference `badLine` without a null check in some scan paths. However, both `GoodTOItems` and `BadTOItems` are projected from the same source lines with matching material/line identifiers. Given the current list construction, a matching `goodLine` should have a corresponding `badLine`. A nullable dereference is therefore not established as reachable in the normal flow and is not reported as a confirmed bug. Defensive null checks may still be useful if the list-building behavior changes.

Relevant list construction:

- `Mobile.MAUI/Components/Pages/Receiving/TransferOrder/TransferOrderItemView.razor.cs:65-142`
- `Mobile.MAUI/Components/Pages/Packing/TransferOrder/TransferOrderItemView.razor.cs:140-242`

### General unsaved-navigation warning suppression

Disposal clears the dirty flag, but that alone does not establish that normal navigation bypasses the confirmation component. The confirmed concern is limited to the singleton flag consumed by session-expiry handling and needs a timing test.

### Weight and return behavior

These remain policy-dependent risks. The code demonstrates prompts and aggregate updates, but not the required accounting rule. Do not alter behavior until operations confirms whether weight is mandatory and whether moved/removed quantities should be re-weighed.

## Recommended Investigation Order

1. Confirm good/bad line selection behavior with repeated-SKU orders, then correct the receiving lookup if the test demonstrates wrong-line attribution.
2. Confirm weight accounting rules for move and negate; test that quantity/weight totals remain valid after partial operations.
3. Confirm whether returns require weight for every scan.
4. Test broadcast dispatch with rapid scans; serialize or guard handlers if concurrent execution is possible.
5. Confirm packing behavior at exactly the packable quantity.
6. Test session expiry after a dirty page is disposed; adjust dirty-flag lifecycle if the warning is lost.

## Conclusion

The source review identifies one concrete selection inconsistency and several important workflow questions with potential inventory impact. The severity of these items should be determined through targeted device/runtime tests and confirmation of the intended business rules. The previous review's nullable-dereference claim and general navigation-warning claim should not be treated as confirmed findings.
