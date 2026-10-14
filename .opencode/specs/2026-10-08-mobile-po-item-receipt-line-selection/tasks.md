# Tasks — Mobile PO Item Receipt: Explicit Line Selection in Payload

**Date:** 2026-10-08
**Status:** Confirmed
**Mode:** BUGFIX
**Spec:** [design.md](./design.md)
**Related:** `fix-netsuite-item-receipt-error-visibility`

## Goal

Make the mobile PO item receipt payload explicitly select lines (`itemreceive` true/false for every line) so NetSuite's transform no longer default-receives omitted lines — eliminating the `[USER_ERROR] Please configure the inventory detail` failure and the silent over-receipt risk on GOOD and BAD posts.

**Architecture summary:** Two-file change in the outbound NS integration path: the payload factory (`PurchaseOrderIRPayloadDTO`) emits all lines; the orchestrator (`SavePOItemReceipt`) stops pre-filtering and passes the full list. No contract, URL, auth, or MediatR changes.

**Verified technical context:** .NET 8; xUnit in `Integration.NS.Tests` (sequential, `AssemblyInfo.cs` module initializer sets `ACCOUNT_ID`); serializer drops nulls (`NetSuiteApiClientService.cs:181-186`); web reference implementation at `ReceivingIntegration.cs:952-1005`.

## Global Constraints

- Follow `.claude/code_conventions.md`: one class per file, async throughout, no exceptions for expected domain failures.
- Per `CLAUDE.md`, create branch `claude/fix/mobile-po-item-receipt-line-selection` before changes; commits land there, never on `master` or the user's branch.
- Do not touch `inventoryStatus` values, bin resolution, URLs, or any file outside those listed below.
- `Integration.NS.Tests` runs with test parallelization disabled — keep new tests side-effect-free (pure factory calls only).

## Phase 0 — Setup

- **T0.1** Create working branch `claude/fix/mobile-po-item-receipt-line-selection` from the current branch.
  `_Requirements: 3.3`

## Phase 1 — Test-First: Demonstrate the Defect

- **T1.1** Create `Integration.NS.Tests/Payloads/PurchaseOrderIRPayloadDTOTests.cs` with a local `JsonSerializerOptions` mirroring `NetSuiteApiClientService.cs:181-186` (`PropertyNamingPolicy = null`, `WhenWritingNull`, `WriteIndented = true`).
  `_Requirements: 2.1, 2.2`
- **T1.2** Write the mixed-PO test: lines L1 (good, qty 10), L2 (zero scanned), L3 (bad, qty 2). Assert the GOOD payload (category 1) serializes with **all three** lines present, L1 `itemreceive: true` with quantity/inventoryDetail, L2 and L3 `itemreceive: false` with no quantity/inventoryDetail/rate/weights. **This test must fail on current code** (current factory emits only passed lines) — that failure is the defect demonstration.
  `_Requirements: 2.1, 2.2, 2.5`
- **T1.3** Write the mirror BAD-payload test (category 2): L3 received, L1/L2 `itemreceive: false`; assert GOOD/BAD received-line sets are disjoint (P5).
  `_Requirements: 2.5`
- **T1.4** Write preservation tests: single-line fully-received PO (category 1) produces field-identical JSON to the current shape (orderLine, quantity, `custcol_dbti_actual_qty`, weights, rate null, `inventoryDetail.inventoryAssignment.items[0]` with `inventoryStatus.id = "1"`, binNumber chain, quantity); and a zero-qty-only list produces all-`false` entries.
  `_Requirements: 2.3, 2.4`
- **Checkpoint:** run `dotnet test --filter PurchaseOrderIRPayloadDTOTests` — T1.2/T1.3 fail, T1.4 passes on unfixed code. Do not proceed until confirmed.

## Phase 2 — Fix

- **T2.1** `Application.DataTransferObjects/Transactions/Receiving/NS/PurchaseOrderIRPayloadDTO.cs` (`CreateForItemReceipt`, lines 23-139):
  - Derive `bool isGoodPost = receivingCategory == 1;` and predicate `line.ScannedQuantity > 0 && (isGoodPost ? !line.IsBad : line.IsBad)`.
  - Map **every** input line: received lines keep the existing fields exactly as at `:96-134` (orderLine, quantity, `PhysicaQuantity`, weights, rate rule `line.IsBad ? 0 : null`, inventoryDetail with `inventoryStatus "1"` and the binNumber chain at `:117-128`); non-received lines emit only `orderLine` + `itemreceive = false` (reuse the shape of the dead branch at `:40-47`, now live).
  - Remove the now-redundant `if (line.ScannedQuantity == 0)` branch — the predicate subsumes it.
  - Do **not** change `InventoryStatus.Id = "1"` (Option A).
  `_Requirements: 2.1, 2.2, 2.3, 3.1`
- **T2.2** `Integration.NS/Services/NetSuiteApiClientService.cs` (`SavePOItemReceipt`, lines 884-927):
  - Keep bucket guards for *whether* to post: `badPO.Any()` / `goodPO.Any()` computed from the same predicates (`IsBad && ScannedQuantity > 0` / `!IsBad && ScannedQuantity > 0`).
  - Pass the **full** `Data` list to both `CreateForItemReceipt` calls (`:896`, `:914`) instead of the filtered subsets.
  - Leave URL construction (`:888`), try/catch rewraps (`:904`, `:922`), and ordering (BAD first, then GOOD) untouched.
  `_Requirements: 2.1, 2.5, 2.6, 3.2, 3.3`
- **Checkpoint:** `dotnet build` clean; `dotnet test` — all Phase 1 tests now pass, full `Integration.NS.Tests` suite green.

## Phase 3 — Verification

- **T3.1** `dotnet build` at solution root — zero errors/warnings introduced. `_Requirements: 3.1-3.3`
- **T3.2** `dotnet test` — entire `Integration.NS.Tests` suite passes (token, resilience, limiter, DI, caching tests unaffected). `_Requirements: 2.6, 3.2`
- **T3.3** Manual staging verification (requires staging NetSuite account):
  1. Mobile receipt on a mixed PO (good + bad + one unscanned line, at least one bin-tracked item) → both posts succeed, no inventory-detail error.
  2. NetSuite UI: GOOD receipt lists only good lines at scanned quantities; BAD receipt lists only bad lines.
  3. Single-line PO receipt → behaves exactly as before the fix.
  `_Requirements: 2.1, 2.4, 2.5`
- **T3.4** Inspect the logged payload (G1 logging from the prior spec) for the staging run and confirm every line appears with explicit `itemreceive`. `_Requirements: 2.1`

## Optional (separate follow-up)

- **T4.1** *Optional — do not include in this change:* follow-up spec to align BAD-post `inventoryStatus` with the web path (status 3 vs hardcoded `"1"` at `PurchaseOrderIRPayloadDTO.cs:115`), pending confirmation of the account's status configuration.

## Traceability Summary

| Requirement | Tasks |
|---|---|
| 2.1 all lines emitted | T1.2, T2.1, T2.2, T3.3, T3.4 |
| 2.2 unselected → false, no qty fields | T1.2, T2.1 |
| 2.3 received lines unchanged | T1.4, T2.1 |
| 2.4 single-line regression | T1.4, T3.3 |
| 2.5 GOOD/BAD disjoint | T1.3, T2.2, T3.3 |
| 2.6 result/error behavior | T2.2, T3.2 |
| 3.1-3.3 preservation | T0.1, T2.1, T2.2, T3.1 |
