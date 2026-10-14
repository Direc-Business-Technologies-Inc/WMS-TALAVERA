# Requirements — Mobile PO Item Receipt Fails on Unselected Lines

**Date:** 2026-10-08
**Status:** Confirmed
**Mode:** BUGFIX
**Scope:** Mobile PO item receipt posting path (`SavePOItemReceipt` → `PurchaseOrderIRPayloadDTO`)
**Related:** `fix-netsuite-item-receipt-error-visibility` (error visibility, implemented — this spec builds on it and does not reopen it)

## Glossary

- **Item Receipt (IR):** NetSuite transaction recording received stock against a source document.
- **Transform:** NetSuite REST operation `POST /record/v1/purchaseOrder/{id}/!transform/itemReceipt`.
- **Inventory Detail:** Per-line subrecord assigning stock to bin + inventory status + quantity.
- **GOOD/BAD post:** Separate IR posts per receiving category (1 = Good, 2 = Bad).
- **Unselected line:** A PO line not part of the current post (zero scanned qty, or scanned under the other status).

## Bug Condition & Current Behavior

- **1.** When a PO has ≥2 lines and the user receives only some of them (or splits good/bad), the mobile GOOD post fails with `[USER_ERROR] Please configure the inventory detail in line N of the item list`.
  - **1.1** Current: `SavePOItemReceipt` (`NetSuiteApiClientService.cs:890,908`) pre-filters lines to `ScannedQuantity > 0` per status before building the payload.
  - **1.2** Current: `PurchaseOrderIRPayloadDTO.CreateForItemReceipt` therefore emits only selected lines in `item.items`; unselected lines are absent.
  - **1.3** Current: NetSuite defaults absent lines to `itemreceive = true`; bin/lot-tracked unselected lines then fail inventory-detail validation.
- **2.** The same defect affects the BAD post: after a successful GOOD post, the BAD transform would default-receive the good lines again (double-receipt risk).

## Acceptance Criteria

- **2.1** WHEN a PO item receipt is posted (GOOD or BAD) from mobile, THEN the payload's `item.items` SHALL contain an entry for **every** open PO line returned by the source query.
- **2.2** WHEN a line is not part of the current post (zero scanned qty or belongs to the other status bucket), THEN its entry SHALL set `itemreceive = false` and SHALL NOT include `quantity`, `inventoryDetail`, `rate`, or weight fields.
- **2.3** WHEN a line is part of the current post, THEN its entry SHALL be unchanged from current behavior (`itemreceive = true`, quantity, inventory detail with bin/status/qty).
- **2.4** WHEN a PO has only one line and it is fully received, THEN the payload SHALL be byte-equivalent in field content to today's payload (no regression for the simple case).
- **2.5** WHEN both GOOD and BAD posts are made for the same PO, THEN each payload SHALL mark the other bucket's lines as `itemreceive = false`, so neither post re-receives the other's lines.
- **2.6** WHEN the transform succeeds, THEN the returned result and error handling SHALL be unchanged (`bool` result, existing exception wrapping at `NetSuiteApiClientService.cs:904,922`).

## In Scope

- `PurchaseOrderIRPayloadDTO.CreateForItemReceipt` — emit all lines with explicit `itemreceive`.
- `SavePOItemReceipt` — stop pre-filtering lines out of the payload input (pass all lines to both posts; the DTO decides received vs. not).
- Unit tests in `Integration.NS.Tests` covering payload shape.

## Out of Scope

- Web path (`ReceivingIntegration.CreatePOJson`) — already correct; untouched.
- TO/Returns receipt paths (`SaveTOItemReceipt`, restlet payloads) — separate flows, not implicated by this error.
- BAD payload `inventoryStatus` value (hardcoded `"1"` at `PurchaseOrderIRPayloadDTO.cs:115`) — **preserved per user decision (Option A)**; alignment with the web path (status 3) is flagged for a follow-up spec.
- `binNumber` null fallback behavior — unchanged.
- `AlreadyFulfilled` field usage — unchanged.
- Any architectural debt from `.claude/architectural_debts.md`.

## Preservation Requirements

- **3.1** Existing field values for received lines (orderLine, quantity, weights, `custcol_dbti_actual_qty`, rate, inventory detail shape) SHALL NOT change.
- **3.2** The transform URL, auth, and `MakeRequest` pipeline SHALL NOT change.
- **3.3** The `PostPurchaseOrderIRCmd` MediatR flow and its callers SHALL NOT change signatures.
