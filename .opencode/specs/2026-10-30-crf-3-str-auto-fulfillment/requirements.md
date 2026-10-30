# CRF 3 — STR Auto-Fulfillment: Requirements

| Metadata | Value |
|---|---|
| Date | 2026-10-30 |
| Status | Confirmed |
| Mode | FEATURE |
| Scope | WMS auto-fulfillment of NetSuite-approved STRs with one active, exact Yes/Yes Fulfillment Exemption rule |
| Related | `.opencode/specs/2026-10-22-str-packing-line-read-hardening/`; `.opencode/specs/2026-10-16-to-partial-fulfillment-if-fix/plan.md`; `.opencode/specs/2026-10-29-printed-document-indicator/` |

**Revision note (2026-10-30):** At the user's request following the spec review, requirements 1.2, 1.5–1.6, 2.2–2.3, 2.5–2.6, 3.1, 3.5–3.6, 4.1–4.2 and the external prerequisites were clarified. NetSuite approval, the existing custom record, Yes/Yes-only WMS automation and direct-to-Shipped fulfillment without a trip ticket are unchanged.

## Introduction

All Stock Transfer Request (STR) categories are eligible for *evaluation*, not just Returns. NetSuite owns approval and the proposed diagram's **Post Approved STR** step. WMS automates fulfillment only for an approved STR with an active maintenance rule matching category, source location and destination location and with both **Is Automated Packing** and **Is Automated Shipping** set to Yes. The fulfillment goes directly to **Shipped** without creating a trip ticket. The proposed diagram's “Automate Trip Ticket” box is superseded by the business clarification. Other rules retain existing processing.

## Glossary

- **STR:** A WMS stock transfer request represented by a NetSuite transfer or intercompany transfer order.
- **Fulfillment Exemption rule:** A NetSuite `customrecord_fulfillment_excemption` record controlling category/location eligibility and automated actions. The script ID's spelling is intentional and follows the supplied Field Explorer screenshot.
- **Exact match:** Same NetSuite internal IDs for transfer category, from location and to location; the rule is active.
- **Yes/Yes:** Both the rule's automated-packing and automated-shipping List/Record fields resolve to Yes. Their stored values are not yet verified; they must not be treated as checkbox `T`/`F` by assumption.
- **IF:** NetSuite Item Fulfillment. **Shipped** corresponds to the fulfillment state WMS currently posts with `shipStatus = "C"`; this feature does not create a trip ticket.
- **Label Template:** The rule's selected Advanced PDF/HTML Template. Selecting a template is not proof of physical printing.
- **Trip-ticket exemption:** A separate existing NetSuite rule used by manual packing; it does not substitute for Fulfillment Exemption eligibility.

## User stories

- As a warehouse user, I want approved STRs on configured Yes/Yes routes to be fulfilled without manual packing and shipping data entry.
- As a NetSuite administrator, I want to maintain eligible categories and location pairs in the existing custom record rather than in WMS code.
- As an operator, I want failed or uncertain automated fulfillments to be identifiable and recoverable without duplicate shipments.

## 1. Rule eligibility and maintenance

**1.1** WHEN any STR category reaches NetSuite's authoritative Approved state, THEN WMS SHALL evaluate active `customrecord_fulfillment_excemption` rules against its transfer category, from location and to location using NetSuite internal IDs.

**1.2** WHEN no active exact match exists, or a matching rule is not Yes/Yes, THEN this feature SHALL take no automatic fulfillment action; existing handling SHALL continue. This is a normal **skip**, not a processing exception.

**1.3** WHEN a matching rule exists, THEN WMS SHALL read `custrecord_fe_transfer_categories`, `custrecord_fe_from_location`, `custrecord_fe_to_location`, `custrecord_fe_is_automated_packing`, `custrecord_fe_is_automated_shipping` and `custrecord_fe_label_template`. A first-release rule SHALL be eligible only when **both** automation values are verified Yes. Mixed combinations are valid configuration but receive no *new* WMS automation in this release.

**1.4** WHEN more than one active rule matches, or a required rule value is missing or unrecognized, THEN WMS SHALL stop automation for that STR and surface a configuration exception rather than choose a rule arbitrarily.

**1.5** The following reference-table rows SHALL be supported as *configuration*, not hard-coded branches:

| Transfer category | From location | To location | Auto Pack | Auto Ship | Label template / printout |
|---|---|---|---|---|---|
| Transfer Order | Old Warehouse - Main | Selling Area - Cauayan | Yes | Yes | Transfer Ticket |
| Return - Good | Selling Area - Cauayan | Old Warehouse - Main | Yes | Yes | Transfer Ticket |
| Return - Bad | Old Warehouse - Main | BO Warehouse - Main | Yes | Yes | Transfer Ticket |

An unconfigured category/location combination, including Intercompany Transfer, SHALL receive no new WMS automation. The names above are reference labels; implementation matches IDs.

**1.6** WHEN automation is enabled for the first time or for a new route, THEN WMS SHALL NOT automatically process STRs approved before that route's recorded activation cutoff, unless those historical STRs are explicitly selected for a controlled backfill. Changes to an older approved STR SHALL NOT silently make it newly eligible. IF an authoritative approval time or reliable pre-activation baseline cannot be established, THEN that route SHALL remain disabled until a safe cutover method is proven.

## 2. Approval and inventory gate

**2.1** WHEN an STR is submitted for approval but has not reached NetSuite's authoritative Approved state, THEN WMS SHALL NOT auto-fulfill it. NetSuite SHALL remain responsible for approval and Post Approved STR; WMS SHALL not change their transitions.

**2.2** BEFORE the first automatic fulfillment POST for an STR, WMS SHALL re-read current approval, **every intended open line**, remaining quantities and required stock/inventory information, including applicable bins, inventory status and units. It SHALL NOT trust a stale create/edit screen or an incomplete line set.

**2.3** WHEN quantity, line state or required inventory assignments cannot be verified safely, THEN WMS SHALL NOT report successful auto-fulfillment or silently substitute another quantity. It SHALL retain an actionable exception for staff. A matched Yes/Yes rule with a failed safety check is **blocked**, unlike a normal no-rule skip.

**2.4** WHEN automatic processing fails, THEN WMS SHALL represent NetSuite approval and fulfillment outcomes separately; failure SHALL NOT be assumed to reverse approval or reopen the STR for editing.

**2.5** WHEN any intended open line fails availability, status, bin, UOM or remaining-quantity validation, THEN WMS SHALL block the **entire automatic attempt before its first IF POST**, rather than omit that line and ship the others. A previously partially fulfilled STR may be processed only for its verified *remaining* open quantities. IF separate IF POSTs are required and a later POST fails, THEN WMS SHALL record and reconcile the resulting partial/uncertain outcome; it SHALL NOT imply that prior successful NS POSTs were rolled back.

**2.6** WHEN an unattended line needs an inventory condition (Good, Bad or other applicable status), THEN WMS SHALL derive it from verified authoritative NetSuite line data or a business-approved deterministic category rule. It SHALL NOT silently override an existing per-line condition based solely on the STR category. WHEN the required condition cannot be determined, THEN WMS SHALL block the attempt before posting.

## 3. Fulfillment, shipping and duplicate safety

**3.1** WHEN a unique Yes/Yes rule matches an approved STR and **all intended open lines** pass the checks in 2.2, 2.5 and 2.6, THEN WMS SHALL create IFs only for their verified still-open quantities without a manual packing submission.

**3.2** WHEN WMS creates a Yes/Yes automatic IF, THEN it SHALL create it directly as **Shipped** and SHALL NOT create a trip ticket, regardless of the separate trip-ticket-exemption setting.

**3.3** WHEN the STR has been partly or fully fulfilled, THEN WMS SHALL account for existing IFs and SHALL NOT intentionally fulfill or ship the same quantity twice.

**3.4** WHEN discovery or processing is repeated, THEN WMS SHALL not blindly create another IF. An uncertain NetSuite POST outcome SHALL require verification/reconciliation before any retry.

**3.5** WHEN automatic processing is in progress, THEN WMS manual packing SHALL not concurrently post a duplicate IF for that STR. This SHALL be enforced by the shared order-scoped claim in 3.6, not by a read-only check of the other path's state.

**3.6** BEFORE either a WMS manual packing path or the WMS automatic path makes an IF POST, THEN it SHALL acquire the **same exclusive claim for the NS STR ID** and re-read current NS fulfillment state after acquisition. The claim SHALL remain effective through that path's posting attempt and outcome recording. IF a WMS path cannot acquire it, THEN that path SHALL not POST. This coordinates WMS writers; separately initiated NetSuite writes still require NS reconciliation.

## 4. Printout and operational visibility

**4.1** WHEN a Yes/Yes rule selects a Label Template, THEN the appropriate printout SHALL be available using **that configured template** through the verified NetSuite/WMS integration. The design SHALL identify which side applies the template; WMS SHALL pass its ID only if that contract requires it. The flow SHALL NOT generate every pictured printout as an additional document or claim that a physical print took place. IF the template cannot be honored, THEN the route SHALL remain disabled until the integration is verified.

**4.2** WHEN automation succeeds, is skipped or fails, THEN authorized staff SHALL be able to identify the STR, the outcome, and an actionable failure or reconciliation reason through an existing authorized operational report/status surface or a minimal new read-only view if none exists. An unmatched or mixed-setting STR SHALL be identified as **skipped**, not as an exception; a matched but unsafe STR SHALL show its blocking reason.

**4.3** WHEN a template is selected or a print link is made available, THEN WMS SHALL NOT represent the document as physically printed or increment a printed count merely because of this feature. Print tracking belongs to the separate printed-document-indicator work.

## 5. Preservation and exclusions

**5.1** Unmatched and mixed-setting STRs SHALL keep their existing NetSuite/manual handling; existing trip-ticket-exemption and manual packing/ship-status behavior SHALL remain unchanged.

**5.2** Existing partial-fulfillment, open-line, bin, inventory-status, UOM and payload protections SHALL remain effective for auto-fulfillment; category-specific field behavior SHALL be verified rather than inferred from another category.

**5.3** Automatic trip-ticket creation and destination item receipt SHALL be out of scope. Destination receiving remains a separate step.

**5.4** WMS SHALL consume the existing NetSuite Fulfillment Exemption custom record, not add a duplicate maintenance record or WMS maintenance UI. NetSuite approval workflow changes, new WMS mixed-setting automation, and physical-print/count persistence SHALL be out of scope.

## External contract prerequisites

Before enabling the feature, confirm with a saved sandbox rule: the `Y/N` list's stored Yes value, transfer-category list mapping to STR category IDs, source/destination IDs, the authoritative Approved status and approval-time/baseline data for safe cutover, record permissions, the Shipped transform per order type, the source-order-to-IF reconciliation lookup, the source of per-line inventory condition, and how the chosen label template is applied to make the correct printout available. The screenshot establishes field script IDs but not these values or behaviors.
