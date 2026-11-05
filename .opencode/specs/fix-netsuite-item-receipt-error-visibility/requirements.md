# NetSuite Item Receipt Error Visibility — Requirements Document

**Spec name:** `fix-netsuite-item-receipt-error-visibility`
**Mode:** BUGFIX
**Status:** Confirmed
**Date:** 2026-10-06

## Introduction

Receiving STRs (Transfer Orders) through the Web item-receipt page fails with an
unactionable message:

```
Create Item Receipt failed: Error posting items: Exception of type 'System.Exception' was thrown.
```

The real NetSuite error response is discarded inside the API client, which makes every
possible root cause (invalid bin, unknown transfer order line, missing fulfillment,
authorization failure, governance limit) indistinguishable to the operator and to
support.

This spec covers the diagnostic fix: surface, parse, and log the true error. The
underlying NetSuite rejection itself is diagnosed *after* this fix ships, because the
RESTlet that validates the payload (script 1853) is deployed in NetSuite and is not
present in this repository.

## Glossary

| Term | Definition |
|------|------------|
| **IR** | Item Receipt — NetSuite transaction recording received inventory against a source document. |
| **STR** | Stock Transfer Request — the internal transaction type; receives against a Transfer Order. |
| **TO** | Transfer Order — the NetSuite source document for a transfer. |
| **IF** | Item Fulfillment — the shipment document linked to a Transfer Order. |
| **RESTlet** | NetSuite custom script HTTP endpoint. Script 1853 (deploy 1) handles TO item receipts. |
| **OAuth1_Path** | `MakeRequestOAuth1` — token-based auth used for RESTlet calls (STR/TO/Returns receipts). |
| **Bearer_Path** | `MakeRequest` — OAuth2 bearer-token calls to the NetSuite REST record API (PO receipts). |
| **GetNetSuiteErrorDetail** | Parser in `NetSuiteApiClientService` that extracts a human-readable message from a NetSuite error response body. |
| **AppActionFactory** | Web-layer wrapper that toasts `"{ActionName} failed: {ex.Message}"`. |
| **orderLine** | Payload field identifying the source document line the receipt applies to. |
| **binNumber** | Payload field identifying the inventory bin for a receipt detail line. |
| **Bug_Condition (C)** | The condition that triggers a bug. |
| **Property (P)** | The desired behavior when the bug condition holds. |
| **Preservation** | Existing behavior that must remain unchanged. |

## Bug Analysis

### Current Behavior (Defect)

#### Bug Category 1: Real error discarded at throw site

- **1.1** WHEN a NetSuite HTTP call returns a non-2xx status AND the response body is
  empty, non-JSON, or matches no known error shape, THEN `GetNetSuiteErrorDetail`
  returns `null` AND `MakeRequestOAuth1` throws `new Exception(null)`, producing
  "Exception of type 'System.Exception' was thrown."
- **1.2** WHEN the error body uses the RESTlet shape
  `{ "error": { "code": ..., "message": ... } }` (error as an **object**), THEN
  `GetNetSuiteErrorDetail` fails to parse it (it only accepts `error` as a **string**)
  and returns null, triggering 1.1.
- **1.3** WHEN any NetSuite HTTP request fails or succeeds on the OAuth1_Path, THEN no
  log record of the URL, status code, or response body exists anywhere, making
  post-incident diagnosis impossible.

#### Bug Category 2: Latent stack-overflow in a DTO used by the failing flow

- **1.4** WHEN `ItemReceiptLineDTO.QuantityOpen` is evaluated, THEN a
  `StackOverflowException` occurs because the property references itself
  (`QuantityPlanned - QuantityOpen` instead of subtracting the received quantity).

#### Bug Category 3: orderLine identifier divergence (hypothesized)

- **1.5** WHEN loading item-receipt lines for an Item Fulfillment source, THEN
  `LineNumber` SHALL resolve to a valid Transfer Order transaction-line id; today it is
  produced by a 4-table correlated subquery (`IF_TO_LINE_ID`) requiring a
  `'RECEIVING'`-type line inside the Transfer Order transaction and joined through
  `transferorderitemlineid`, which can yield no row, while every other receiver path
  (including mobile) uses the source document's own `tl.id`.

#### Bug Category 4: binNumber type divergence (hypothesized)

- **1.6** WHEN posting a transfer-order receipt from web, THEN `binNumber` is sent as a
  bin *number string* (`y.Bin?.BinNumber`) whereas mobile omits the field entirely and
  the PO path sends the bin *internal id* (`y.Bin?.Id`).

### Expected Behavior (Correct)

#### Bug Category 1: True error surfaced

- **2.1** WHEN a non-2xx response contains a parseable error detail, THEN the thrown
  exception message SHALL contain that detail AND the HTTP status code. On the
  Bearer_Path the existing `NetSuiteErrorResponse.DisplayString` behavior is
  preserved; the status-plus-body fallback applies only when `DisplayString` is
  null or empty.
- **2.2** WHEN a non-2xx response body is empty, non-JSON, or unrecognized, THEN the
  thrown exception message SHALL contain the HTTP status code AND the raw response body
  truncated to a bounded length, never a null-derived generic message. On the
  Bearer_Path the raw-body fallback SHALL be derived from the already-read response
  string with its own try/catch, NOT a second `ReadFromJsonAsync` content read
  (a second read can throw `JsonException` and mask the real status).
- **2.3** WHEN the error body uses the RESTlet object shape
  `{ "error": { "code", "message" } }`, THEN `GetNetSuiteErrorDetail` SHALL return the
  `error.message` value.
- **2.4** WHEN a NetSuite HTTP call fails on either path, THEN the system SHALL log a
  warning containing the URL, HTTP status code, and response body, following the
  existing `ILogger<T>` injection pattern.
- **2.5** WHEN an exception propagates out of `PostItemReceipt`, THEN the original
  exception SHALL be preserved as the inner exception rather than only its message
  string being re-wrapped. The final aggregate throw SHALL carry the collected
  exceptions as its inner (e.g. an `AggregateException`) so the stack chain is not
  lost. WHEN payload construction itself throws before any post task is created
  (`tasks` is empty), THEN that construction exception SHALL be rethrown rather than
  swallowed into a `true` return.

#### Bug Category 2: Recursion resolved

- **2.6** WHEN `ItemReceiptLineDTO.QuantityOpen` is evaluated, THEN it SHALL return
  planned minus received quantity without infinite recursion.

#### Bug Category 3 & 4: Evidence-driven payload correction

- **2.7** WHEN the actual NetSuite rejection indicates an invalid or unknown source
  line, THEN the payload's `orderLine` SHALL be sourced directly from the Item
  Fulfillment line's own `transferorderitemlineid` instead of the correlated subquery.
- **2.8** WHEN the actual NetSuite rejection indicates an invalid bin reference, THEN
  `binNumber` SHALL be aligned with the payload variant proven to work on the failing
  flow.
- **2.9** WHEN no fulfillment id is present on the web form submit, THEN the submit
  SHALL be refused with an actionable validation message instead of sending a
  sentinel value, AND the `fulfillmentId` value actually sent SHALL be included in
  the Stage 1 warning log so Branch C is classifiable from T7 evidence.

### Unchanged Behavior (Regression Prevention)

- **3.1** WHEN a NetSuite call succeeds (2xx), THE system SHALL CONTINUE TO deserialize
  the response identically, including returning default on an empty success body.
- **3.2** WHEN an error body matches the already-supported shapes
  (`o:errorDetails[].detail`, `error` as string), THE system SHALL CONTINUE TO parse
  them exactly as before.
- **3.3** WHEN the PO receipt path (Bearer_Path) posts, THE system SHALL CONTINUE TO
  behave identically — no changes to payload construction (`CreatePOJson`,
  `CreateReturnsJson`) in this fix.
- **3.4** WHEN the web form submits a valid item receipt, THE system SHALL CONTINUE TO
  show the success toast and navigate exactly as before (`AppActionFactory` behavior
  unchanged).
- **3.5** WHEN `ItemReceiptLineDTO.QuantityOpen` is fixed, THE system SHALL CONTINUE TO
  compile and behave identically everywhere the property is not evaluated.
- **3.6** WHEN the `orderLine` source is changed per 2.7, THE system SHALL CONTINUE TO
  send the same value for correctly-linked fulfillments.

## Scope

### In-Scope

- `MakeRequestOAuth1` / `MakeRequest` throw-site: status code + body fallback.
- `GetNetSuiteErrorDetail`: RESTlet object-shape parsing plus `title`/`message`
  fallbacks.
- Warning-level request/response logging on both HTTP paths in
  `NetSuiteApiClientService`.
- Inner-exception preservation and dead-filter removal in
  `ReceivingIntegration.PostItemReceipt`.
- `ItemReceiptLineDTO.QuantityOpen` self-reference fix.
- The Stage 2 diagnostic gate procedure (manual) and the conditional payload fixes it
  selects.

### Out-of-Scope

- Payload content changes taken on assumption rather than evidence (Bug Categories 3
  and 4 are implemented only for the branch the Stage 2 diagnostic gate confirms).
- Introducing a test project or test framework.
- Concurrent GOOD/BAD/MISSING posting and partial-write rollback (architectural change).
- OAuth1 semaphore / governance gating (performance hardening).
- Refactoring the multi-line `match` expressions in `CreateTOJson` / `CreateReturnsJson`.
- `ItemReceiptPackingHandler.SubmitLinesAsync` remaining untranslated in the packing
  path (separate defect, same family).
