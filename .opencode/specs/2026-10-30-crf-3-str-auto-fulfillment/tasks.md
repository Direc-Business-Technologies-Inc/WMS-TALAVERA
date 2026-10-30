# CRF 3 — STR Auto-Fulfillment: Implementation Tasks

| Metadata | Value |
|---|---|
| Date | 2026-10-30 |
| Status | Confirmed |
| Mode | FEATURE |
| Scope | WMS auto-fulfillment of NetSuite-approved STRs with one active, exact Yes/Yes Fulfillment Exemption rule |
| Spec | [design.md](design.md); confirmed [requirements.md](requirements.md) |
| Related | `.opencode/specs/2026-10-22-str-packing-line-read-hardening/`; `.opencode/specs/2026-10-16-to-partial-fulfillment-if-fix/plan.md`; `.opencode/specs/2026-10-29-printed-document-indicator/` |

**Task-plan amendment (user-requested review, 2026-10-30):** Tasks 1–2 and 4–9 now enforce first-activation cutover, whole-attempt line validation, shared manual/automatic posting claims, verified per-line inventory condition and availability of the configured printout. NetSuite still owns approval and the maintenance record; WMS automation remains Yes/Yes-only and directly Shipped without a trip ticket.

**Goal:** Safely ship only newly eligible approved STRs matching one active Yes/Yes NetSuite maintenance rule, with no trip ticket or duplicate IF. **Architecture:** bounded WMS polling with a per-route activation cutoff → fresh NS rule/order reads → shared order-scoped WMS posting claim → whole-attempt validation → guarded NS Shipped IF → verified outcome/operational status. The existing NS approval and rule-maintenance UI are external dependencies, not WMS implementation work.

**Verified technical context:** `Integration.NS` and `Database.MsSql` target .NET 8; `Api.CoreWebAPI` targets .NET 9 (`*.csproj:4-5`), and the solution includes `Integration.NS.Tests` (`WMS.slnx:24-38`). NetSuite access is in `Integration.NS`; use MediatR application use cases and project DI. Avoid an `ITransactionalRequest` enclosing remote NS calls. The actual DB registration reads `ConnectionStrings_AddonDB` or `ConnectionStrings:AddonDB` (`Database.MsSql/DatabaseMsSqlDI.cs:19-25`), contrary to the older guide's `DefaultConnection` name. Do not put credentials in committed config or change unrelated architectural debts.

## Phase A — Prove the external contract

### 1. Verify NetSuite rule and approval values

- In sandbox, save a Yes/Yes `customrecord_fulfillment_excemption` rule and an approved STR. Verify the six `custrecord_fe_*` fields named in `requirements.md`, the Y/N list's stored Yes value, transfer-category list mapping, location IDs, record permissions and authoritative approval-status value. Locate the NS-approved candidate source, an authoritative approval-time field or safe pre-activation baseline, and paging/scope requirements; the existing `Integration.NS/Implementations/Transactions/StockTransferRequestIntegration.cs:234-279` read projects status/IDs but does not filter to Approved, and the Returns grid at `:80-118` is not an approval feed.
- Prove that an old approved STR modified after activation stays excluded unless explicitly approved for backfill. Establish a route-specific activation cutoff before adding live rules or enabling the worker; do not treat `lastmodifieddate` as approval time without proof.
- Capture the proven query shapes and test data **without credentials**. A missing, inactive, ambiguous, submitted or rejected STR must not qualify.
- **Checkpoint:** sandbox evidence proves a unique eligible match, excludes an unapproved document **and an older approved document**, and establishes a safe route cutover. Do not hard-code the `Y/N` Yes ID before this.

_Requirements: 1.1–1.6, 2.1._

### 2. Verify Shipped IF, inventory and reconciliation in sandbox

- Exercise the three configured example routes and each source record type to be enabled; confirm NetSuite's accepted `transferOrder/{id}/!transform/itemFulfillment` behavior for intercompany and return orders, direct `shipStatus = "C"`, statuses/bins/UOM, partial open quantities and a suitable source-order-to-IF lookup. Prove how an unattended Returns-Good line carrying a Bad/Missing condition is identified; the manual per-line override at `Application.DataTransferObjects/Transactions/Commons/NS/Payload/InventoryStatusMap.cs:27-49` must not silently disappear. The existing TO/Returns posting implementations are at `Integration.NS/Services/NetSuiteApiClientService.cs:2929-2992,3163-3213`.
- Establish which NetSuite/WMS side uses `custrecord_fe_label_template` and prove that the **configured** printout is available. Do not change print payloads until its contract is verified; if it cannot be honored, leave the route disabled. Confirm what an HTTP success with empty/uncertain response means and how an IF can be verified.
- **Checkpoint:** no category can be enabled unless Shipped posting, existing-IF detection, full-order stock/assignment/condition reads and the configured printout are demonstrated; record unsupported cases as blocked.

_Requirements: 2.2–2.3, 2.5–2.6, 3.1–3.4, 4.1, 5.2._

## Phase B — Implement eligibility and durable state

### 3. Add typed NS integration contracts and rule/approval reads

- Add feature-specific interface(s) under `Application.UseCases/Repositories/Integration/` and DTOs under `Application.DataTransferObjects/Transactions/StockTransferRequest/` (new file names are implementation choices). Implement under `Integration.NS/Implementations/Transactions/` and register in `Integration.NS/Implementations/NSImplementationDI.cs:132-145`.
- Read active custom-record rules by **internal IDs** with the verified Yes mapping. Query a fresh authoritative approved status and page complete candidate/open-line sets. Reject missing/unrecognized/duplicate rule results. Do not reuse a cached UI list or depend on an interactive user's subsidiary claims.
- Add contract/query tests under `Integration.NS.Tests/Implementations/Transactions/`; assert exact Yes/Yes, mixed, unmatched, inactive, conflicting and unapproved behavior.
- **Checkpoint:** rule tests pass and any uncertain field value fails closed without posting.

_Requirements: 1.1–1.5, 2.1, 5.1._

### 4. Add a WMS processing record, unique claim and migration

- Add a processing entity under `Domain.Entities/Entities/`, EF `IEntityTypeConfiguration<T>` under `Database.MsSql/Configurations/`, and `DbSet` in `Database.MsSql/Core/AppDbContext.cs:13-26`. Add a **new** migration under `Database.MsSql/Migrations/`; inspect it before applying. Verify the design-time project/startup and environment setup rather than using the guide's nonexistent `MsSql` path verbatim.
- Define an application repository contract and MSSQL implementation; register it in `Database.MsSql/DatabaseMsSqlDI.cs:17-39`. Enforce a unique NetSuite STR/action processing key **and** an order-scoped exclusive posting claim available to both manual and auto WMS paths. Model atomic acquisition, renewal, release and claim expiry explicitly; maintain the lease through NS posting/outcome recording without holding a DB transaction across the NS call. Record Skipped/Blocked/OutcomeUncertain/Completed states and verified IF IDs where available; reconcile stale/uncertain results against NS before retrying.
- Test concurrent auto claims, **manual-first and auto-first acquisition**, crash/lease-expiry and uncertain outcomes with a suitable real database fixture if available; do not infer database uniqueness from mocked in-memory behavior alone.
- **Checkpoint:** at most one WMS path owns the order posting claim at a time, and an uncertain POST never becomes an automatic second POST solely because its lease expires.

_Requirements: 2.3–2.4, 3.3–3.6, 4.2._

## Phase C — Fulfillment and hosting

### 5. Implement the automatic application use case

- Add a dedicated command/handler under `Application.UseCases/Commands/Transaction/StockTransferRequest/`. MediatR scans handlers in `Application.UseCases/AppUseCasesDI.cs:19-25`. Input is an NS STR internal ID; after acquiring the shared posting claim, re-read approval, exact rule, existing IFs and **every intended open line**. Verify full remaining quantities and the stock/bin/UOM/inventory condition required for each line; use authoritative per-line condition or an explicitly business-approved category rule. If any intended line fails, return a Blocked outcome **before the first IF POST**; do not silently omit it and ship the rest. For an earlier partial shipment, validate only the verified remaining open quantities.
- Preserve the field-proven filler/payload behavior in `Application.DataTransferObjects/Transactions/Packing/NS/Payload/TransferOrderIFPayloadDTO.cs:30-63`. The TO read/reconciliation guards (`NetSuiteApiClientService.cs:2749-2816`) and Returns path (`:3163-3213`) are not equivalent: implement the unattended path's required fail-closed checks rather than simply invoking the Returns save.
- If multiple NS IF POSTs are needed, track each verified result; a later POST failure must leave a partial/uncertain outcome for reconciliation, never a claim that earlier posts were rolled back.
- Add use-case tests for order/category variants, mixed Good/Bad condition, single-invalid-line whole-attempt block, earlier partial fulfillment, overship, missing bins, failed authoritative reads and a later-POST failure.
- **Checkpoint:** every failed *pre-post* validation yields zero NS IF POST calls; a failure after a successful POST is reported and reconciled as a partial/uncertain outcome.

_Requirements: 2.1–2.6, 3.1, 3.3–3.4, 5.2._

### 6. Add explicit auto-ship posting and manual competition guard

- Extend the NetSuite posting interface and `Integration.NS/Services/NetSuiteApiClientService.cs` with an explicit **automatic Shipped** intent. Do not set `IsTripTicketExempt = true` as a workaround; manual TO/Returns shipping decisions currently use that independent setting at `:2892-2894,3172-3174`.
- Before manual posting in `Application.UseCases/Commands/Transaction/Packing/NS/TransferOrder/PostTransferOrderIFCmd.cs:19-49` and the Returns counterpart, **acquire the same order-scoped exclusive claim** used by automation; a read-only check of the auto worker's state is not sufficient. The manual commands currently use `ITransactionalRequest` (`PostTransferOrderIFCmd.cs:11`, `PostReturnsIFCmd.cs:12`): establish a committed, independently scoped short claim transaction *before* the NS POST instead of relying on their ambient transaction to commit after the remote call. Verify this boundary without changing manual fulfillment semantics. Reject a conflict with an actionable message, re-read NS fulfillment state after acquiring the claim, and hold/renew the claim until the posting outcome is known. Protect all WMS entry points that share these commands; NS-side concurrent writes remain a sandbox release gate.
- Extend payload/save-guard tests under `Integration.NS.Tests/Services/` and `Integration.NS.Tests/Payloads/` for Shipped, no trip ticket, correct line set, both claim acquisition orders and unchanged manual exempt/non-exempt status.
- **Checkpoint:** auto-path posts Shipped independently of exemption; manual regression tests pass, and manual/auto WMS saves cannot both acquire posting rights for the same STR.

_Requirements: 3.1–3.6, 5.1–5.3._

### 7. Host bounded, disabled-by-default approved-STR discovery

- Add a hosted worker in `Api.CoreWebAPI/` and register it from `Api.CoreWebAPI/Registers/WebApplicationService.cs:14-35`. Create a DI scope per batch; explicitly configure NS subsidiary scope, stable paging, batch size, polling interval and restart lookback. **Persist a route-specific first-activation cutoff** using verified approval time, or establish a provable pre-activation baseline when approval time is not exposed. Treat historical backfill as an explicit, bounded operator action; do not classify an older approval as new because its `lastmodifieddate` changed. Use Task 4's shared claim for multi-instance safety and a default-off rollout switch. The API host already uses `builder.ConfigureServices()` (`Api.CoreWebAPI/Program.cs:5-11`); no new public callback endpoint is required.
- Test overlapping polls, instance restarts, duplicate candidates, pre-activation approved orders modified later, deliberate backfill and transient NS outages. Unknown approval or rule values fail closed; never depend on missing HTTP user claims to authorize a background scan.
- **Checkpoint:** feature off makes no automatic NS writes; first activation excludes older approvals, while restart cannot blindly duplicate or silently lose the post-cutover candidate window.

_Requirements: 1.1–1.2, 1.6, 2.1, 3.3–3.6._

## Phase D — Visibility, acceptance and release

### 8. Expose an actionable processing outcome to authorized staff

- Investigate existing authorized operational/admin reports first; use an existing surface if it can show processing outcomes. Only if none is suitable, add a minimal read-only WMS view (its file location must be discovered at implementation). Surface order ID, **normal no-rule/mixed skip**, verified IF reference(s), blocked pre-post reason, and whether a POST may have completed and needs NS reconciliation. Follow the established `BaseComponent` → feature handler → MediatR and permission conventions if a Web component is added. Logs alone do not satisfy staff visibility; do not add a blind-retry button.
- **Checkpoint:** authorized staff can distinguish a normal skip, a matched Yes/Yes safety failure and a potentially completed NS POST; there is no false printed-status/count claim.

_Requirements: 2.3–2.4, 4.1–4.3._

### 9. Validate, pilot and reconcile

- Run `dotnet build Api.CoreWebAPI/Api.CoreWebAPI.csproj` and `dotnet test Integration.NS.Tests/Integration.NS.Tests.csproj`; extend testing for rule mapping, approved gate, activation cutoff and backfill, all enabled categories, per-line condition and whole-order preflight, UOM/status/bin checks, partial orders and partial IF POST outcomes, duplicate discovery, **manual-first and auto-first shared claim**, lost POST response, and exact NS JSON payload. Add database claim verification against the real configured SQL Server test environment when available; do not invent a passing result.
- Execute one sandbox end-to-end approved STR and rule per enabled route, inspect actual IFs and **availability of the configured printout**, then pilot one example route with the worker switch enabled *after* recording its activation cutoff. Reconcile NS IFs against WMS processing outcomes before broad rollout. Back out by disabling the worker, not by deleting uncertain processing records.
- **Checkpoint:** all confirmed correctness properties in `design.md` hold in tests and sandbox; each enabled route has an observed successful or blocked outcome with no unintended trip ticket or historical auto-shipment.

_Requirements: 1.1–5.4, including 1.6, 2.5–2.6 and 3.6._

## Optional later work

If NetSuite provides a verified authenticated approval callback contract, replace or supplement polling with a callback that invokes the **same** idempotent application use case; do not build a second fulfillment path. Mixed-setting WMS automation and destination receiving are separate future requirements.

_Requirements: 2.1, 3.4._
