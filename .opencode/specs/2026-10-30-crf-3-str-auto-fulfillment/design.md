# CRF 3 — STR Auto-Fulfillment: Design

| Metadata | Value |
|---|---|
| Date | 2026-10-30 |
| Status | Confirmed |
| Mode | FEATURE |
| Scope | WMS auto-fulfillment of NetSuite-approved STRs with one active, exact Yes/Yes Fulfillment Exemption rule |
| Spec | [requirements.md](requirements.md); implementation plan in [tasks.md](tasks.md) |
| Related | `.opencode/specs/2026-10-22-str-packing-line-read-hardening/`; `.opencode/specs/2026-10-16-to-partial-fulfillment-if-fix/plan.md`; `.opencode/specs/2026-10-29-printed-document-indicator/` |

**Decision corrections (user-requested review amendment, 2026-10-30):** D1 now requires a route-specific cutover baseline; D3 requires complete preflight and explicit inventory-condition resolution; D4 uses a shared exclusive claim for **both** manual and automatic WMS posts; D5 requires the configured printout to be available. These tighten the original design without changing NetSuite approval ownership or Yes/Yes-only direct-to-Shipped behavior.

## Decision record

| Decision | Problem and choice | Rationale / trade-off |
|---|---|---|
| D1 — Discovery | Propose a bounded WMS poller for NetSuite-approved STRs, with explicit subsidiary scope, route-specific first-activation cutoff and restart lookback. Re-read approval before posting. Older approvals need an explicitly approved backfill; a later modification does not reset approval age. | No approval callback/contract is verified in this repository. Polling adds delay and a hosted worker; a future authenticated callback may invoke the *same* processing use case. A route remains off if authoritative approval time or an equivalent safe pre-activation baseline cannot be proven. [Req. 1.6] |
| D2 — Eligibility | Read active `customrecord_fulfillment_excemption` rules and require a unique category/from/to match with *verified* Yes/Yes values. | The record and six field script IDs are visible in NetSuite screenshots, but no saved records or `Y/N` internal IDs are available yet. Mixed values receive no new WMS automation. [Req. 1.1–1.5] |
| D3 — Posting | Add a dedicated auto-fulfillment use case with an explicit **Shipped** decision. Validate *every intended open line* and derive the required inventory condition before the first IF POST; if any line fails, block the whole attempt. Reuse proven low-level guards and payload contracts where appropriate. | Existing manual commands choose ship status from trip-ticket exemption; forcing that flag to true would change its meaning. The manual Returns path allows per-line condition to outrank category (`InventoryStatusMap.cs:27-49`); unattended posting must not guess. Multiple NS POSTs cannot be rolled back together. [Req. 2.2–2.6, 3.1–3.2, 5.1–5.2] |
| D4 — Recovery | Persist an automation outcome per NS STR/action plus a **shared exclusive order-scoped posting claim** used by both manual and automatic WMS IF paths. Each path acquires it *before* its own NS re-read and POST; ambiguous HTTP outcomes are quarantined for reconciliation. | A read-only check followed by POST has a race even if the auto worker claims its own job. Use short database transactions for claim transitions, maintain the lease across the external call without holding the transaction open, and do not equate this with an NS inventory lock or atomic cross-system write. [Req. 3.3–3.6, 4.2] |
| D5 — Templates | Read the rule's label template ID and verify whether NetSuite applies it or WMS must pass it; the configured printout must be available before the route is enabled. | The current fulfillment `custbody_dbti_print_label` field is a Boolean, not proof of template selection or printing. Avoid additional printouts and physical-print claims. [Req. 4.1–4.3] |

## What already exists (verified)

| Capability | Source | Consequence |
|---|---|---|
| STR category, source/destination IDs and custom approval status read | `Integration.NS/Implementations/Transactions/StockTransferRequestIntegration.cs:234-279` | Extend integration with an authoritative approved-only, identity-scoped discovery read. Submission merely PATCHes a flag (`:411-427`). |
| Returns list admits categories 3/4 without approval filter | `Integration.NS/Implementations/Transactions/StockTransferRequestIntegration.cs:80-118` | Do not use this grid as the approval feed. |
| Manual TO IF command resolves trip-ticket exemption | `Application.UseCases/Commands/Transaction/Packing/NS/TransferOrder/PostTransferOrderIFCmd.cs:19-49` | Do not route auto-ship through the manual command unchanged. |
| Manual IF state and label flag | `Integration.NS/Services/NetSuiteApiClientService.cs:2892-2894,3172-3174` | Manual status depends on exemption; auto path needs explicit Shipped intent. |
| TO fresh-open-line reconciliation and partial-order guards | `Integration.NS/Services/NetSuiteApiClientService.cs:2749-2816`; `Application.DataTransferObjects/Transactions/Packing/NS/Payload/TransferOrderIFPayloadDTO.cs:30-63` | Preserve the verified sublist/filler wire shape and fail closed for unattended posting. |
| Returns IF save has different, weaker freshness checks | `Integration.NS/Services/NetSuiteApiClientService.cs:3163-3213` | Do not infer it provides TO-equivalent current-stock and open-line guarantees. |
| API host/DI and MSSQL model | `Api.CoreWebAPI/Registers/WebApplicationService.cs:14-35`; `Database.MsSql/Core/AppDbContext.cs:13-37` | Host a scoped worker and add durable processing state via a new migration. |
| JWT protects existing user-facing mobile packing endpoint | `Api.CoreWebAPI/Controllers/Packing/TransferOrderController.cs:15-20,95-101` | Background NS access must not assume an interactive JWT or user claims. |

**Change boundary:** Add NetSuite rule/discovery readers, application orchestration, processing state/worker and an operational status surface. Preserve manual packing, separate trip-ticket exemption, NetSuite approval and destination receipt. This feature uses `Integration.NS`; the SAP guidance does not apply. No new WMS maintenance UI is required.

## Flow

```mermaid
sequenceDiagram
    participant NS as NetSuite
    participant W as WMS poller / use case
    participant DB as WMS processing state
    NS->>NS: Approve and post STR
    W->>NS: Page approved STR IDs after route cutover (or explicit backfill)
    W->>NS: Re-read approval, IDs, rule and open lines
    alt No matching Yes/Yes rule
        W->>DB: Normal skip; existing handling continues
    else Ambiguous or invalid rule
        W->>DB: Configuration exception; no IF POST
    else Unique Yes/Yes match
        W->>DB: Acquire shared order-scoped posting claim
        W->>NS: Check existing IFs and every intended line / inventory condition
        alt All lines validated
            W->>NS: Create IF directly as Shipped
            W->>NS: Verify resulting IF
            W->>DB: Record verified outcome
        else Unsafe or POST outcome uncertain
            W->>DB: Record actionable exception; no blind retry
        end
    end
```

## Components, contracts and state

1. **NetSuite integration:** Introduce typed rule, approval/candidate, open-line, existing-IF and posting contracts under `Application.UseCases/Repositories/Integration/` and `Application.DataTransferObjects/Transactions/StockTransferRequest/`; implement NS reads/writes under `Integration.NS`. Query `customrecord_fulfillment_excemption` with the six `custrecord_fe_*` field IDs in the requirements. Use NS internal IDs, an active filter, explicit subsidiary scope and bounded stable paging. Read required stock/status/bin/UOM and any per-line condition from current NS state; reject incomplete or unsupported line sets. Sandbox must establish category/list value mapping and record type/transform compatibility.
2. **Application processor:** A new MediatR use case takes an NS STR internal ID, not browser-provided category or locations. After claiming, re-read eligibility, existing IFs, **every intended still-open line** and required status/stock/bin/UOM; block the whole attempt before its first IF POST if any line fails. A previously partly fulfilled order contributes only verified remaining quantities. Where multiple IF posts are required, record a partially completed/uncertain result if a later post fails and reconcile each committed IF. Post Shipped with explicit auto-ship intent and verify/store IF reference(s); never create a trip ticket. Avoid wrapping remote HTTP calls inside `ITransactionalRequest`: its database behavior commits only after handler completion (`Application.UseCases/Behaviors/TransactionalDocumentBehavior.cs:11-31`), which is not an NS transaction.
3. **Local durable state:** Add a processing entity, EF configuration, unique NS STR/action key and an order-scoped exclusive posting claim shared by manual and automatic WMS writers. Acquire/renew/release its lease with short DB transactions; keep it effective during NS calls, and do not let expiry turn an uncertain result into an automatic repost. Record states such as `Claimed`, `Blocked`, `OutcomeUncertain` and `Completed` plus diagnostic timestamps and verified IF IDs. Reconcile completed/uncertain records against NS before any new action.
4. **Manual coordination:** Manual TO/Returns saves and the auto processor must both acquire the **same** order-scoped claim, then re-read NS state before their IF POST; neither may proceed on a failed claim. A check of an auto worker's claim *without acquiring* is insufficient. The existing manual commands use `ITransactionalRequest` (`PostTransferOrderIFCmd.cs:11`, `PostReturnsIFCmd.cs:12`); the shared claim must become committed and visible through a **separate short transaction before the external POST**, rather than remaining inside the manual command's ambient transaction. Preserve manual behavior while verifying that boundary. A WMS claim is not an NS inventory reservation; account-level concurrent NS edits remain a sandbox/release risk to validate.
5. **Hosting/operations:** Register a bounded, default-off hosted poller in the API host. Record a first-activation cutoff per route, using authoritative approval time or a proven pre-activation baseline; old approvals need an explicit backfill. A later edit to an old approved STR must not bypass the cutoff. Create a DI scope per batch, use an overlapping discovery window and the unique claim to tolerate restarts/multiple instances, and record separate skipped/blocked/uncertain/completed outcomes. Use an existing authorized operational report/status surface where sufficient; add a read-only view only if necessary. No new public API endpoint or NetSuite-to-WMS callback is required for the polling design.
6. **Template selection:** Determine whether NS applies the rule's `custrecord_fe_label_template` or WMS must pass it. A route cannot be enabled until the configured printout is demonstrably available. The current `custbody_dbti_print_label` flag only makes IFs eligible for the packing-label list (`Integration.NS/Implementations/Transactions/Packing/ReturnPackingIntegration.cs:226-243`); physical printing/counting is separate.

## Correctness properties

1. **Eligibility and cutover [1.1–1.6]:** Only one active exact ID match with verified Yes/Yes and a post-activation approval (or explicitly approved backfill) is a candidate; unmatched/mixed/old approvals do not auto-post, and ambiguous rules fail closed.
2. **Approval [2.1]:** Submitted, rejected or otherwise unapproved STRs never auto-post.
3. **Whole-attempt inventory preflight [2.2–2.3, 2.5–2.6, 5.2]:** No first IF POST follows an incomplete or failed check on **any** intended open line or unknown per-line condition; existing partial shipments contribute only verified remaining quantities. If later NS POSTs fail, record partial/uncertain results rather than implying rollback.
4. **Shipping [3.1–3.2]:** A successful Yes/Yes automatic IF is Shipped and has no new trip ticket, independent of trip-ticket exemption.
5. **Recovery [3.3–3.6]:** Repeated discovery and concurrent WMS manual/automatic writers cannot both acquire the same order claim; ambiguous results require NS reconciliation, not a blind repost.
6. **Printout and preservation [4.1–4.3, 5.1–5.4]:** The configured template makes the correct printout available without claiming physical printing; manual statuses and trip-ticket behavior stay as before, mixed/unmatched rules get no new automation.

## Validation and error behavior

- Test rule mapping, approval transitions, missing/conflicting/inactive/mixed rules, old approvals edited after activation, approved historical backfill, whole-attempt line and quantity checks, mixed Good/Bad conditions, bin/status/UOM, partial orders, multiple workers, both order-claim acquisition orders (manual-first and auto-first), lost POST response and partial outcome reconciliation. Tests belong near existing `Integration.NS.Tests` query/payload/guard tests, plus use-case/DB claim tests where those components are implemented.
- **Property-based testing: APPLICABLE, optional.** Generated category/location/flag combinations can test exact-match and no-post invariants. Prioritize deterministic NetSuite payload and concurrency tests; no new testing dependency is required by this specification.
- Distinguish `Skipped`, `Blocked before POST`, `Completed` and `POST outcome uncertain` to staff. Failure does not undo NS approval. Unknown approval/rule values, unavailable required reads and unresolved IF outcome fail closed. Avoid secrets in diagnostics.
- Production gates: one saved sandbox Yes/Yes rule; confirmed Yes and category mappings; approved-only discovery with proven route cutover/baseline; proven Shipped transform for each enabled order type; source-order IF reconciliation; full-order stock/bin/status/UOM and per-line condition checks; configured printout available; verified shared claim against both WMS paths; an observed pilot and rollback switch. Until verified, do not enable the worker.

## Accepted consequences and exclusions

Polling makes fulfillment eventual rather than synchronous with NetSuite approval. SQL claims reduce WMS overlap but cannot make the WMS-to-NS POST atomic; uncertain outcomes need operator reconciliation. Excluded: NetSuite approval changes, new maintenance UI, mixed-setting automation, automatic trip tickets, destination receipt, and physical print-status/count persistence. Known unrelated architectural debts remain untouched.
