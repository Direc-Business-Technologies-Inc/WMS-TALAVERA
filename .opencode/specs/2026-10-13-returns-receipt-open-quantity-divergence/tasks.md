# Returns/STR Item Receipt Open-Quantity Divergence — Tasks Document

**Spec name:** `fix-returns-receipt-open-quantity-divergence`
**Mode:** BUGFIX
**Status:** Confirmed
**Date:** 2026-10-13
**Spec:** `design.md` (sibling)

## Goal

Make the web IF-path open-quantity math read the TO line's `quantityshiprecv`, and add a
server-side over-receipt guard in `PostItemReceipt`, so mixed mobile→web receiving no
longer produces NetSuite 400s.

## Architecture Summary

Two layers, both inside
`Integration.NS/Implementations/Transactions/ReceivingIntegration.cs`:

1. Page load: IF-path `QuantityReceived` derived from the resolved TO RECEIVING line's
   `quantityshiprecv` (component 2 of the design), replacing `IF_RECEIPTS_QUERY`.
2. Submit: `PostItemReceipt` re-reads TO line planned vs received and refuses any line
   whose allotted quantity exceeds the remainder, before any HTTP post.

## Technical Context

- TO, ITO, and Returns (good/bad) share these code paths: `GetItemReceiptHeaderAsync`
  filters `t.type IN ('TrnfrOrd','PurchOrd')` (`ReceivingIntegration.cs:538`), returns are
  flagged by `custbody_dbti_transfer_category IN (3,4)` (`:531`). No recordtype branching
  is needed or wanted (req 3.8).
- `ItemReceiptDTO.SourceTypes.TransferOrder` and `.Returns` together cover every non-PO web
  receipt; `.PurchaseOrder` must stay untouched (req 3.1).
- SuiteQL queries are built with `builderFactory.Create()` and parameterized filters
  (`.claude/code_conventions.md` — no string interpolation into SQL).
- `ILogger<ReceivingIntegration>` is already injected (`ReceivingIntegration.cs:37`).
- Validation failures inside `PostItemReceipt` already throw before any post; the
  `tasks.Count == 0` branch (`:766-775`) logs and rethrows, so a guard throw follows an
  established pattern rather than inventing one.

## Global Constraints

- All changes confined to `Integration.NS/Implementations/Transactions/ReceivingIntegration.cs`
  — no DTO, ViewModel, component, mobile, or `NetSuiteApiClientService` changes (P4–P6).
- async/await throughout; no `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`.
- Do not modify restlet 1853, the PO path, or anything in `.claude/architectural_debts.md`.
- Keep the fix minimal: the IF-dialog filtering (req 2.4) stays deferred unless the user
  lifts the deferral.
- Work on a `claude/fix/` branch per `CLAUDE.md` before implementation.
- No test project exists; verification is `dotnet build` plus the manual scenarios in
  Phase C.

## Phase A — Root Fix (QuantityReceived source)

### Task 1 — Add the `IF_TO_LINE_RECEIVED_QTY` constant

_Requirements: 2.1, 3.8_

- File: `Integration.NS/Implementations/Transactions/ReceivingIntegration.cs`, beside
  `IF_RECEIPTS_QUERY` / `IF_TO_LINE_ID` (`:561-588`).
- Add a `const string` that wraps the existing `IF_TO_LINE_ID` resolution:
  ```csharp
  const string IF_TO_LINE_RECEIVED_QTY = """
      SELECT NVL(rline.quantityshiprecv / uom.conversionrate, 0)
      FROM transactionline rline
      WHERE rline.id = ( {IF_TO_LINE_ID} )
      """;
  ```
  Interpolate the existing `IF_TO_LINE_ID` constant into the placeholder at the call site,
  mirroring how `:597` and `:610` already compose their select items.
- Acceptance: the constant compiles and the composed SQL executes in NetSuite SuiteQL.

### Task 2 — Swap the `QuantityReceived` source

_Requirements: 2.1, 3.2, 3.6, 3.8_

- File/line: `ReceivingIntegration.cs:610` inside
  `GetItemReceiptItemFulfillmentLinesAsync` — replace
  `($"({IF_RECEIPTS_QUERY})", "QuantityReceived")` with
  `($"({IF_TO_LINE_RECEIVED_QTY})", "QuantityReceived")`.
- Leave the `LineNumber` select item (`:597`, `IF_TO_LINE_ID`) and every other select,
  join, and filter untouched.
- Delete the now-unreferenced `IF_RECEIPTS_QUERY` constant (`:561-572`).
- Acceptance: `IF_RECEIPTS_QUERY` no longer appears in the repository; `GetItemReceiptLinesAsync`
  (the non-IF path) is unchanged.

**Checkpoint:** `dotnet build` from the solution root (T1), then run T2 on sandbox — the
mobile-full → web IF page must show open quantity 0.

## Phase B — Server-side over-receipt guard

### Task 3 — Add `GetTransferOrderLineRemainingAsync`

_Requirements: 2.2, 3.8_

- File: same class, placed near `GetItemReceiptLinesAsync` (`:630`).
- Query via `builderFactory`: select `tl.id` (line id), `item.itemid` (item code),
  `item.displayname` (item description), `ABS(tl.quantity / NVL(uom.conversionrate,1))` as
  planned, `(tl.quantityshiprecv / NVL(uom.conversionrate,1))` as received.
- From `transactionline tl` joined to `transaction t` and `item i`; filters:
  `Equal("t.id", sourceInternalId)`,
  `In("t.recordtype", new[] { "transferorder", "intercompanytransferorder" })`,
  `Equal("tl.mainline", "F")`, `Equal("tl.transactionlinetype", "RECEIVING")`.
- Return a file-private record (line id, item code, item description, planned, received).
  No `Application.DataTransferObjects` changes.
- Acceptance: returns rows for a TO and for an ITO in sandbox (P8).

### Task 4 — Guard block in `PostItemReceipt`

_Requirements: 2.2, 2.3, 3.1, 3.7_

- File/line: `ReceivingIntegration.cs:679`, inside the existing `try`, immediately after
  the orderLine fail-fast guard (`:693-701`).
- Skip entirely when `dto.SourceType == ItemReceiptDTO.SourceTypes.PurchaseOrder` (P4).
- Otherwise call the Task 3 helper with `dto.SourceInternalId`. For each dto line with any
  allotted quantity, sum `QuantityAlloted` across all `InventoryDetails`; if the sum exceeds
  `planned − received`:
  1. `logger.LogWarning` with source reference, item code, allotted, remaining, received
     (req 2.3);
  2. throw the message defined in the design's Error Handling section.
- Lines absent from the helper result are not validated — the orderLine=0 guard already
  covers unresolvable lines (req 3.6).
- Acceptance: a forced submit against a stale page is rejected with an actionable toast, a
  warning log entry is present, and no restlet request is made (confirmed by the absence of
  the payload log at `:741-746`).

**Checkpoint:** `dotnet build` from the solution root (T1).

## Phase C — Verification (manual, sandbox)

### Task 5 — Execute T1–T6

_Requirements: 2.1, 2.2, 2.3, 3.1–3.8_

- T1 — build all projects; compiles with no new warnings.
- T2 — mobile-full receipt then web IF page (the reported incident): open quantity 0, form
  blocks, guard rejects a forced submit, no 400.
- T3 — mobile-partial receipt then web continuation: only the remaining quantity is
  offered and the post succeeds.
- T4 — pure-web partial receipt then a second web continuation: the second load shows the
  correct remaining quantity (this run also resolves the restlet-linkage hypothesis in the
  design).
- T5 — ITO and returns-bad variants of T2/T3 behave identically to TO.
- T6 — PO receipt, non-IF web load, and a mobile receipt are unchanged.
- Record outcomes; add `implementation-notes.md` only if requested after implementation,
  from actual changed files and verification evidence.

### Task 6 — Optional (deferred): IF-dialog filtering

_Requirements: 2.4_

- **Optional — do not implement unless the deferral is lifted.** Extend
  `GetSTRItemFulfillments` (`ReceivingIntegration.cs:959-990`) to exclude fulfillments whose
  TO lines have no remaining receivable quantity, reusing the Task 3 query shape.

## Traceability Summary

- Req 2.1 → Tasks 1, 2 · Req 2.2 → Tasks 3, 4 · Req 2.3 → Task 4 · Req 2.4 → Task 6
  (Optional, deferred)
- Req 3.1 → Task 4 (PO skip) · Req 3.2 → Task 2 (non-IF path untouched) · Req 3.3 → Task 5/T6
  (mobile unchanged) · Req 3.4 → Task 5/T3 (payload contract) · Req 3.5 → Task 5/T6 ·
  Req 3.6 → Task 2, Task 4 · Req 3.7 → Task 4, accepted in design · Req 3.8 → Tasks 2, 3, 5/T5