# NetSuite Item Receipt Error Visibility — Design Document

**Spec name:** `fix-netsuite-item-receipt-error-visibility`
**Mode:** BUGFIX
**Status:** Confirmed
**Date:** 2026-10-06

## Overview

The web STR item-receipt flow fails with an unactionable message because
`NetSuiteApiClientService.MakeRequestOAuth1` discards NetSuite's error response and
throws `new Exception(null)`.

This design fixes error visibility unconditionally, then gates the actual payload
correction behind observed evidence from NetSuite's own Script Execution Log.

## Design Considerations

- The RESTlet (script 1853) is deployed in NetSuite, not in this repository, so its
  validation contract cannot be verified statically. Payload changes must therefore be
  evidence-driven, not assumption-driven.
- `Integration.NS` currently has no logging at all. Introducing `ILogger<T>` follows the
  existing handler-level convention (`PostTransferOrderIFCmd.cs:16`).
- No test projects exist in the solution; verification is manual plus build validation.
  Adding a test framework is out of scope.
- Several live flows share `NetSuiteApiClientService` (PO, Returns, STR, Trip Ticket).
  Stage 1 must be strictly behavior-preserving for the success path.
- The `quantity` and `orderLine` payload fields in `CreateTOJson` were not corrected
  during the preceding refactor (no invoice/`rate`/`amount` fields were ever added there).
  Only the `orderLine` source is conditionally correctable under this spec.

## Requirements Traceability

| Requirement | Design element |
|-------------|----------------|
| 1.1, 1.2, 2.1, 2.2 | Stage 1.2 — status + bounded body in exception message |
| 1.3, 2.4 | Stage 1.4 — warning log with URL / status / body |
| 2.3 | Stage 1.3 — RESTlet object-shape parsing |
| 2.5 | Stage 1.5 — inner-exception preservation in `PostItemReceipt` |
| 1.4, 2.6 | Stage 1.1 — `QuantityOpen` self-reference fix |
| 1.5, 2.7, 3.6 | Stage 3 Branch B — conditional `orderLine` substitution |
| 1.6, 2.8 | Stage 3 Branch A — conditional `binNumber` alignment |
| — (see 2.9) | Stage 3 Branch C — `IfId ?? -1` validation + sent-`fulfillmentId` logging |
| — | Stage 3 Branch D — OAuth1 token/role (NetSuite admin action, non-code) |
| 3.1–3.5 | Stage 1 invariants — no success-path changes |

## Hypothesized Root Cause

### Bug Category 1 — Real error discarded at the throw site (CONFIRMED)

- **Evidence**: `NetSuiteApiClientService.cs:469` throws `new Exception(errorDetail)`
  while the status-code message is commented out at `:468`;
  `GetNetSuiteErrorDetail` returns `null` for any unrecognized body.
- **Validation**: none needed — directly observable in code.

### Bug Category 2 — `QuantityOpen` self-reference (CONFIRMED)

- **Evidence**: `ItemReceiptDTO.cs:68` evaluates `QuantityPlanned - QuantityOpen`.
- **Validation**: none needed.

### Bug Category 3 — orderLine identifier divergence (HYPOTHESIS, NOT CONFIRMED)

- **Suspected Root Cause**: `LineNumber` (`ReceivingIntegration.cs:448`, subquery at
  `:425-439`) resolves through `previoustransactionlinelink` joined to
  `transferorderitemlineid`, additionally filtered on
  `ttl.transactionlinetype = 'RECEIVING'` for a line inside the Transfer Order
  transaction. If that join finds no row, `LineNumber` becomes `0` or fails
  deserialization into the non-nullable `int` at `ItemReceiptDTO.cs:45`, and the restlet
  receives `orderLine: 0`.
- **Corroborating Evidence**: mobile's equivalent payload uses `tl.id`
  (`NS_TransferOrder_Get_Items.sql:19`), a different column entirely; every sibling
  script aliases `tl.id AS LineSequenceNumber`.
- **Validation Approach**: capture the real NetSuite error after the Stage 1 deploy. An
  error referencing an unknown or invalid transfer order line confirms the hypothesis.

### Bug Category 4 — binNumber type divergence (HYPOTHESIS, NOT CONFIRMED)

- **Suspected Root Cause**: web sends `binNumber = y.Bin?.BinNumber` (a string) at
  `ReceivingIntegration.cs:876`; mobile omits the field; `CreatePOJson` sends
  `Bin?.Id` (an int).
- **Validation Approach**: same error capture; an `INVALID_KEY_OR_REF` naming a bin
  confirms it.

## Design

### Architecture

Three stages, with Stage 2 a manual diagnostic gate between them.

**Stage 1 — Observability (unconditional, ships first)**

1. Fix the `QuantityOpen` self-reference.
2. Rebuild the exception message: status code + parsed detail when parseable, otherwise
   status code + a bounded raw-body excerpt. On the Bearer_Path the fallback is derived
   from the already-read response string (single content read) so the status code is
   never masked by a second `ReadFromJsonAsync` throwing `JsonException`.
3. Extend `GetNetSuiteErrorDetail` with the RESTlet object shape plus `message`/`title`
   fallbacks.
4. Inject `ILogger<T>` into `NetSuiteApiClientService`; log a warning on every non-2xx
   with URL, status, and body (including the `fulfillmentId` actually sent on the
   receipt path, so Branch C is classifiable).
5. Preserve the original exception as `InnerException` in `PostItemReceipt` — each
   rethrow carries the caught exception as inner, the final aggregate throw carries the
   collected exceptions, and a payload-construction failure before any post task is
   created (`tasks.Count == 0`) is rethrown instead of swallowed into a `true` return;
   remove the unreachable "Empty response from NetSuite API" filter.

**Stage 2 — Diagnostic gate (manual, no code)**

Deploy Stage 1, reproduce one failing STR receipt, capture the toast text and the
NetSuite Script Execution Log entry for script 1853 / deployment 1, then classify:

| Observed error signal | Branch | Fix location |
|-------|--------|--------------|
| `INVALID_KEY_OR_REF` naming a bin, or a bin-related `USER_ERROR` | A | `ReceivingIntegration.cs:876` |
| Unknown / invalid transfer order line, or `orderLine` reference failure | B | `ReceivingIntegration.cs:448` and `:425-439` |
| Fulfillment reference failure | C | `ItemReceiptCreatePage.razor.cs` (`?? -1`) |
| 403 or permission `USER_ERROR` | D | OAuth1 token / role configuration — NetSuite admin action |
| Nothing logged for script 1853 | E | Request never reached the restlet — URL, token, or environment problem |

**Stage 3 — Conditional payload fix (blocked on Stage 2)**

- **Branch A**: align `binNumber` with the variant proven to work on the failing flow
  (drop the field, or send `Bin?.Id`).
- **Branch B**: replace the correlated `IF_TO_LINE_ID` subquery with the Item Fulfillment
  line's own `tl.transferorderitemlineid`, eliminating the correlated subquery while
  preserving the resolved value for correctly-linked fulfillments (requirement 3.6).
- **Branch C**: remove the `?? -1` fallback and fail fast with a validation message when
  no fulfillment id is present, and ensure the actual `fulfillmentId` value sent is
  logged (requirement 2.9).
- **Branch D**: no code change; NetSuite administrator action.

Only the branch matching observed evidence is implemented. Unmatched branches are
deferred to a follow-up spec.

### Components

- `Integration.NS/Services/NetSuiteApiClientService.cs` — `MakeRequestOAuth1`,
  `MakeRequest`, `GetNetSuiteErrorDetail` (Stage 1.2 through 1.4).
- `Integration.NS/Implementations/Transactions/ReceivingIntegration.cs` —
  `PostItemReceipt` (Stage 1.5), `CreateTOJson` and
  `GetItemReceiptItemFulfillmentLinesAsync` (Stage 3).
- `Application.DataTransferObjects/Transactions/Receiving/ItemReceiptDTO.cs` —
  `QuantityOpen` (Stage 1.1).

### Data Flow

Unchanged in Stage 1: request construction, payload serialization, and response
deserialization are untouched. Only the failure path gains detail. Stage 3 alters only
the `orderLine` value or the `binNumber` field for the transfer-order receipt payload.

### Error Handling

The fix *is* the error handling. After Stage 1, every NetSuite failure yields a message
of the form `NetSuite <code> <reason>: <detail>`, where `<detail>` is the parsed RESTlet
or REST error, or a body excerpt of at most 2000 characters. Nested wrapping in
`ReceivingIntegration` preserves the stack via `InnerException`, so the operator sees the
NetSuite cause while the log retains the full chain.

### Testing Strategy

No test projects exist; verification is manual plus build validation.

| # | Scenario | Expected result |
|---|----------|-----------------|
| T1 | Build all 12 projects | No new warnings introduced by Stage 1 |
| T2 | Successful PO receipt | Unchanged behavior (req 3.3) |
| T3 | Successful web STR receipt for a correctly linked IF | Payload identical to pre-change; success toast (req 3.4) |
| T4 | Reproduce the reported failure | Toast shows `NetSuite <code>: <real detail>`; log entry contains URL, status, and body |
| T5 | Returns and Trip Ticket flows | Unchanged — they share the client |
| T6 | Branch-specific fix after Stage 2 | Targeted change verified against a sandbox receipt |

Property-based testing is not applicable: the logic under repair is I/O-bound error
formatting with no meaningful invariant space, and the restlet contract is external to
this repository.

## Trade-offs

- **Two stages instead of a single fix**: slower to a single "fixed" moment, but it
  prevents a blind payload edit from breaking the PO and Returns flows that share this
  client.
- **Branch B removes the 4-table correlated subquery**: faster and a single source of
  truth, but it changes a query that currently works for some fulfillments — hence
  requirement 3.6 and gate-mandated validation.
- **Introducing `ILogger<T>` into `Integration.NS`**: a small convention shift for that
  project; the alternative (exception text alone) leaves no server-side record.
