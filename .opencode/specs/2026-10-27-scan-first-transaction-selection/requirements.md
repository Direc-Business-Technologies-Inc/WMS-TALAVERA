# Scan-First Transaction Selection — Requirements

| Metadata | Value |
|---|---|
| Date | 2026-10-27 |
| Status | Confirmed |
| Mode | FEATURE |
| Scope | Receiving PO, TO, Returns; Packing TO, Returns, Vendor Return Authorization |
| Related | None found |

## 1. Introduction

Mobile operators should be able to locate a transaction by scanning its barcode or manually entering its transaction number, without waiting for a full pending transaction list to load. On the six in-scope selection pages, the app will request an individual transaction only after operator input.

## 2. Glossary

- **Selection page:** A mobile screen that currently lists pending transactions and lets an operator choose one.
- **Transaction identifier:** The verified value encoded in a transaction barcode or entered manually.
- **Server lookup:** An operation-specific request that resolves one transaction and validates whether it can be processed by the current operator.
- **Detail page:** The existing screen opened after a transaction is selected; it may load that transaction’s item lines.

## 3. Scan-first selection

### 3.1

As an operator, I want to scan a transaction barcode to open that transaction, so I do not have to wait for a list or choose from unrelated transactions.

**Acceptance criteria**

- WHEN an operator scans a valid identifier on an in-scope page, THEN the app SHALL request that transaction from the server and open the existing workflow only if the transaction is eligible.
- WHEN an operator enters a transaction number and submits it, THEN the app SHALL use the same lookup and eligibility checks as scanning.
- WHEN an in-scope page first opens, THEN the app SHALL NOT automatically fetch or render the full pending transaction list.
- WHEN a lookup is in progress, THEN the app SHALL show progress and prevent duplicate submissions until the request completes.

## 4. Lookup outcomes and safety

### 4.1

- WHEN the identifier is empty, unknown, or resolves to the wrong transaction type, THEN the app SHALL show a clear error and remain on the selection page.
- WHEN a transaction is unauthorized, belongs to an unavailable subsidiary, or is no longer eligible for the operation, THEN the server SHALL reject the lookup and the app SHALL NOT open its detail page.
- WHEN a lookup fails due to connectivity or server error, THEN the app SHALL show a recoverable error and allow retry.
- WHEN a valid transaction is returned, THEN the app SHALL preserve existing navigation and transaction-detail behavior.

## 5. In-scope pages

The scan/manual lookup behavior applies to:

1. Receiving → PO (`PurchaseOrderView`).
2. Receiving → TO (`TransferOrderView`).
3. Receiving → Returns (`ReturnsView`).
4. Packing → TO (`TransferOrderView`).
5. Packing → Returns (`ReturnsView`).
6. Packing → Vendor Return Authorization (`VendorReturnAuthorizationView`).

For each page, opening the selector SHALL not automatically load its pending list; scan/manual lookup SHALL replace list-backed selection.

## 6. Preservation requirements

- Item/detail pages SHALL continue loading the selected transaction’s item lines; item-level scanning and saving SHALL not change.
- Inventory Counting selection and worksheet creation SHALL remain unchanged.
- Trip Ticket creation and its multi-fulfillment scan workflow SHALL remain unchanged.
- Authentication and server-side authorization SHALL remain in force; knowledge of an identifier SHALL NOT grant access.
- Transaction status transitions, save operations, and operation-specific business rules SHALL remain unchanged.

## 7. Scope exclusions and safety constraint

Out of scope: item/detail pages; Receiving fulfillment sub-selection pages; Inventory Counting; worksheet creation; Trip Ticket; changes to transaction processing/business rules; and a browse-pending fallback.

Do not convert a page unless its barcode identifier and a safe single-transaction lookup contract are verified. If the identifier does not uniquely resolve the expected eligible transaction, hold that operation out rather than forcing the scan-first flow.
