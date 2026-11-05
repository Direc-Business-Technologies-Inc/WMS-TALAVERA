# Returns/STR Item Receipt Open-Quantity Divergence — Requirements Document

**Spec name:** `fix-returns-receipt-open-quantity-divergence`
**Mode:** BUGFIX
**Status:** Confirmed
**Date:** 2026-10-13
**Scope:** Web item-receipt IF-path open-quantity calculation; server-side over-receipt guard in `PostItemReceipt`; IF-selection filtering (Optional/deferred).
**Related:** `fix-netsuite-item-receipt-error-visibility` (predecessor — shipped error visibility, payload logging, `QuantityOpen` recursion fix, orderLine=0 fail-fast; this spec diagnoses the 400 that fix surfaced).

## Introduction

Receiving a Return (returns-bad/returns-good) or an intercompany transfer order (ITO)
partially on mobile and continuing on the web fails with:

```
Create Item Receipt failed: Error posting items: NetSuite 400 Bad Request error
```

The web item-receipt page, when loaded through an Item Fulfillment, computes "quantity
already received" from Item Receipts linked to the fulfillment. Mobile posts returns
receipts by transforming the Transfer Order, so those receipts are linked to the
Transfer Order instead. The web therefore overstates the open quantity and submits a
receipt NetSuite must reject.

This spec fixes the quantity accounting so every channel reads received quantities from
the same NetSuite-maintained ledger (the Transfer Order line's `quantityshiprecv`), and
adds a server-side guard so no client can post more than remains.

The fix is recordtype-agnostic: Transfer Orders, Intercompany Transfer Orders, and
Returns (good and bad) share the same code paths and are covered together.

## Glossary

| Term | Definition |
|------|------------|
| **IR** | Item Receipt — NetSuite transaction recording received inventory against a source document. |
| **STR** | Stock Transfer Request — internal transaction type; received against a Transfer Order. |
| **TO** | Transfer Order — NetSuite source document for a transfer or return. |
| **ITO** | Intercompany Transfer Order — `t.recordtype = 'intercompanytransferorder'`, `t.type = 'TrnfrOrd'`. |
| **IF** | Item Fulfillment — the shipment document linked to a Transfer Order. |
| **Returns TO** | A Transfer Order with `custbody_dbti_transfer_category` 3 (good) or 4 (bad). |
| **RESTlet 1853** | NetSuite custom script endpoint used by the web for all non-PO receipts (`script=1853&deploy=1`). Deployed in NetSuite; not in this repository. |
| **REST transform** | NetSuite record API `POST /record/v1/transferOrder/{id}/!transform/itemReceipt` — the endpoint mobile uses for returns receipts. |
| **`previoustransactionlinelink`** | NetSuite table linking a source document line to the document created from it (`previousdoc`/`previousline` → `nextdoc`/`nextline`). |
| **`quantityshiprecv`** | NetSuite-maintained cumulative received quantity on a transaction line. |
| **`QuantityOpen` / `QuantityReceived` / `QuantityAlloted`** | Web DTO fields: planned − received / cumulative received / quantity the operator assigns to a status. |
| **`IF_RECEIPTS_QUERY`** | Correlated subquery in `ReceivingIntegration` summing IR quantities linked from an IF line. Replaced by this spec. |
| **`IF_TO_LINE_ID`** | Correlated subquery resolving an IF line to its TO RECEIVING line id. |
| **P (property)** | The desired behavior when the bug condition holds. |
| **Preservation** | Existing behavior that must remain unchanged. |

## Bug Analysis

### Current Behavior (Defect)

- **1.1** WHEN a receipt is posted by any channel that transforms from the Transfer Order
  (mobile returns via `SaveReturnsItemReceipt`, `NetSuiteApiClientService.cs:1018`), THEN
  the created IR is linked TO→IR in `previoustransactionlinelink` AND the TO RECEIVING
  line's `quantityshiprecv` increases AND no IF→IR link is created.
- **1.2** WHEN the web item-receipt page is loaded with an `itemFulfillment` parameter
  (`GetItemReceiptSourceQry.cs:25-27` → `GetItemReceiptItemFulfillmentLinesAsync`,
  `ReceivingIntegration.cs:590`), THEN `QuantityReceived` is computed by
  `IF_RECEIPTS_QUERY` (`ReceivingIntegration.cs:561-572`), which sums only IRs whose
  `previousdoc` is the IF — so receipts posted per 1.1 are not counted.
- **1.3** WHEN `QuantityReceived` undercounts per 1.2, THEN `QuantityOpen`
  (`ItemReceiptDTO.cs:68`) is inflated by exactly the uncounted received quantity AND the
  form's over-receipt guard (`ItemReceiptForm.razor.cs:72`) passes against the inflated
  value.
- **1.4** WHEN the inflated receipt is submitted, THEN `PostItemReceipt`
  (`ReceivingIntegration.cs:679`) posts to RESTlet 1853 with quantities exceeding the TO
  lines' remaining AND NetSuite rejects the request with HTTP 400, surfaced as
  `Create Item Receipt failed: Error posting items: NetSuite 400 Bad Request error`.
- **1.5** WHEN the operator opens the Item Fulfillments dialog
  (`ReturnsDetails.razor:91-100` → `GetSTRItemFulfillments`,
  `ReceivingIntegration.cs:959`), THEN fulfillments whose TO lines have nothing remaining
  are still offered for selection.
- **1.6** WHEN `PostItemReceipt` is invoked, THEN no server-side validation compares
  allotted quantities against the TO lines' remaining quantity before posting.

### Expected Behavior (Correct)

- **2.1** WHEN the web item-receipt page is loaded through an Item Fulfillment, THEN
  `QuantityReceived` SHALL be sourced from the resolved TO RECEIVING line's
  `quantityshiprecv` (via the existing `IF_TO_LINE_ID` resolution) — the same ledger the
  web non-IF path (`ReceivingIntegration.cs:650`) and mobile
  (`NS_TransferOrder_x_Return_Get_Items.sql:30`) read — for `transferorder` and
  `intercompanytransferorder` records alike.
- **2.2** WHEN `PostItemReceipt` is called for a TransferOrder or Returns source, THEN it
  SHALL query the TO lines' planned and received quantities and SHALL refuse to post
  (with an actionable message naming the item and both quantities) any line whose allotted
  quantity exceeds the remaining quantity.
- **2.3** WHEN the guard in 2.2 rejects a receipt, THEN the rejection SHALL be logged
  (warning level, including source reference and per-line quantities) following the
  existing `ILogger<ReceivingIntegration>` pattern.
- **2.4** *(Optional — deferred)* WHEN the Item Fulfillments dialog lists fulfillments for
  a Returns or STR document, THEN fulfillments whose TO lines have no remaining receivable
  quantity SHOULD be excluded. This is UX hardening, not correctness, and ships only if
  the deferral is lifted.

### Unchanged Behavior (Regression Prevention)

- **3.1** WHEN a PO receipt is posted (`CreatePOJson`), THE system SHALL CONTINUE TO behave
  identically — the PO path is untouched.
- **3.2** WHEN the web item-receipt page is loaded without an `itemFulfillment` parameter,
  THE system SHALL CONTINUE TO use `GetItemReceiptLinesAsync` and its
  `quantityshiprecv`-based `QuantityReceived` unchanged.
- **3.3** WHEN mobile posts a returns or TO receipt, THE system SHALL CONTINUE TO behave
  identically — no mobile payload or query changes.
- **3.4** WHEN a valid receipt is posted from any channel, THE system SHALL CONTINUE TO
  succeed with the same payload shape sent to RESTlet 1853 (only the decision of what
  quantity is offerable changes, not the payload contract).
- **3.5** WHEN a NetSuite call fails, THE system SHALL CONTINUE TO surface status code and
  detail per the predecessor spec (`fix-netsuite-item-receipt-error-visibility`,
  requirements 2.1–2.5).
- **3.6** WHEN `orderLine` values are resolved for the IF path, THE system SHALL CONTINUE TO
  use the `IF_TO_LINE_ID` resolution and the existing orderLine=0 fail-fast guard
  (`ReceivingIntegration.cs:693-701`) unchanged.
- **3.7** WHEN a TO line is split across multiple fulfillments, THEN the IF-path open
  quantity SHALL represent the TO line's remaining quantity (the conservative direction: it
  may under-offer but never over-offer), and this semantics change SHALL be documented.
- **3.8** WHEN the source is an ITO, THE system SHALL CONTINUE TO behave identically to a
  TO — no recordtype-specific branching SHALL be introduced.

## Scope

### In-Scope

- `IF_RECEIPTS_QUERY` replacement in `GetItemReceiptItemFulfillmentLinesAsync`
  (`Integration.NS/Implementations/Transactions/ReceivingIntegration.cs`).
- Server-side remaining-quantity guard inside `PostItemReceipt` for non-PO sources.
- Warning logging on guard rejection.
- Manual verification procedure (no test projects exist in the solution).
- Optional, deferred: fulfillment-list filtering in `GetSTRItemFulfillments` (req 2.4).

### Out-of-Scope

- Any change to RESTlet 1853 (deployed in NetSuite, not in this repository).
- Unifying the web returns path onto the REST transform endpoint mobile uses.
- Concurrent GOOD/BAD/MISSING posting, partial-write rollback, OAuth1 governance gating
  (deferred by the predecessor spec; still deferred).
- Mobile payload or query changes.
- Adding a test project or test framework.
- Any item in `.claude/architectural_debts.md`.