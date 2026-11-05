# TO Packing Item Fulfillment Fails on Partially Fulfilled Transfer Orders

**Date:** 2026-10-16
**Status:** Confirmed
**Mode:** BUGFIX (fix + UI polish)
**Scope:** Mobile TO Packing save path → NetSuite Item Fulfillment transform; shared packing query consumers (desktop read-only impact)
**Related:** None found

## Glossary

- **TO** — Transfer Order (NetSuite record `transferOrder`, reference number `ISTR-...`)
- **IF** — Item Fulfillment, created via `!transform/itemFulfillment` on the TO
- **Closed line** — a TO line that is fully shipped or `isclosed = 'T'`; no longer present on the transform's item sublist
- **Remaining qty** — `LineQuantity − quantityshiprecv` for an open line
- **Filler line** — zero-qty payload entry (`isReceived=false`) required for `?replace=item.inventoryDetail.inventoryAssignment` consistency on bin-tracked locations
- **Bucket** — the GOOD or BAD subset of scanned lines posted as one fulfillment call

---

## 1. Problem & Root Cause

**Symptom:** Mobile save on TO `ISTR-THM00100000631` fails with:

```
Action failed: Error while posting GOOD TO Item Fulfillment. NetSuite 400 Bad Request:
Bad Request(400): [USER_ERROR] Error while accessing a resource. You have attempted an
invalid sublist or line item operation. You are either trying to access a field on a
non-existent line or you are trying to add or remove lines from a static sublist.
[diag replace=True usedBin=True scannedLines=5 totalLines=301]
```

**Root cause (confirmed via order status "Pending Receipt / Partially Fulfilled"):** the order was partially fulfilled earlier. The transform's `item` sublist only exposes open lines, but the payload (`TransferOrderIFPayloadDTO.CreateForItemFulfillment`) sends **all** lines as scanned entries or zero-qty fillers. Any `orderLine` referencing a closed line is an invalid insert on a static sublist → NetSuite rejects the entire save.

**Prior fixes (line dedupe at `NetSuiteApiClientService.cs:1092`, bin gating at `:1119`) addressed different failure modes and are unaffected by this change.**

**Known limitation:** the original client failure cannot be reproduced on the exact production order; validation is against sandbox equivalents plus the existing diag output. The failing payload was also logged server-side (`NetSuiteApiClientService.cs:1227`) even though the scan originated on a client device — the client calls the API; it never posts to NetSuite directly.

---

## 2. Requirements

- **2.1** WHEN a TO line is closed or fully shipped, THE save SHALL exclude it from the IF payload (both scanned entries and zero-qty fillers).
- **2.2** WHEN a scan exceeds the *remaining* qty of a partially shipped line, THE save SHALL fail with an actionable message naming the WMS line, material code, ordered and remaining qty — not a NetSuite 400.
- **2.3** WHEN the save is blocked by 2.1/2.2, NO bucket (BAD or GOOD) SHALL be posted — the guard runs before the first NetSuite call.
- **2.4** The mobile UI SHALL show fully-shipped lines in a read-only **DONE** tab; they SHALL NOT appear in GOOD/BAD lists and SHALL NOT be scannable.
- **2.5** Partially shipped lines SHALL remain in GOOD with the remaining qty driving scan validation and display.
- **2.6** Desktop packing (same query via `StockTransferRequestPackingIntegration.cs:195`) SHALL behave as today — flags are added to the query, no visual or behavioral change.
- **2.7** Existing behaviors preserved: line dedupe, bin-resolution gating, trip-ticket exemption check, `?replace=` contract, backorder banner, BAD-before-GOOD posting order.

### Out of scope (deferred)

- Combined single GOOD+BAD post (would eliminate the partial-post window but is a behavioral change requiring regression testing; recorded as follow-up).
- Retry/bucket-completion tracking (see Section 7).
- Removing filler lines / `?replace=` from the payload contract.
- Hiding closed lines on the desktop/Blazor packing pages.

---

## 3. Design

**Decision:** select-and-guard at the data layer + client-side DONE tab. **No changes to `TransferOrderIFPayloadDTO`, `?replace=`, or the filler contract** — the payload builder simply receives clean input. Filtering upstream was chosen over payload modification to minimize blast radius, per review.

```
NS_TO_x_Packing_Get_Items.sql  →  adds tl.isclosed, tl.quantityshiprecv (no row filtering)
        ↓
PostTransferOrderDTO / TransferOrderLineVM  →  new fields IsClosed, QuantityAlreadyShipped
        ↓
Mobile TransferOrderItemView    →  routes closed lines to DONE list (display-only)
        ↓
SaveTOItemFulfillment (NS)      →  guard: strip/validate before first MakeRequest
        ↓
TransferOrderIFPayloadDTO       →  UNCHANGED
```

### Key components

1. **Query** (`Integration.NS\NSScripts\NS_TO_x_Packing_Get_Items.sql`): add `tl.isclosed AS IsClosed`, `tl.quantityshiprecv AS QuantityAlreadyShipped` to the SELECT. Rows are NOT filtered in SQL so each consumer (mobile display, save guard, desktop) decides; filtering in SQL would silently change every consumer's row set.

2. **DTO/VM plumbing:** map the new fields through `StockTransferRequestPackingIntegration` → handler → mobile `TransferOrderLineVM` and `PostTransferOrderDTO`.

3. **Save guard** (`SaveTOItemFulfillment`, `NetSuiteApiClientService.cs:1122`, immediately after dedupe at `:1124`):
   - Drop closed lines (`IsClosed` true, or remaining ≤ 0) from `Data` before bucketing.
   - For each scanned line, reject if `ScannedQuantity > LineQuantity − QuantityAlreadyShipped`, throwing a message naming the WMS line, material code, ordered vs remaining qty — following the existing `goodMissingBin` guard pattern (`:1207`–`1210`).
   - The guard runs before the BAD bucket post, satisfying Req 2.3.

4. **Mobile DONE tab** (`Mobile.MAUI\Components\Pages\Packing\TransferOrder\TransferOrderView.razor` / `TransferOrderItemView.razor.cs`):
   - New list `DoneTOItems` + DONE tab with badge count.
   - Closed lines routed to DONE on load (`OnInitializedAsync` mapping, `:84`–`180`) and in `Refresh()` (`:970`–`1022` — include DONE lines in the scan-state snapshot/restore).
   - DONE lines are display-only: excluded from `GoodTOItems`/`BadTOItems`, from `ItemRequest` barcode fetch, and from all scan/manual/move/negate handlers — a closed `orderLine` is exactly what crashes the transform, so this is business-critical, not cosmetic.
   - Because DONE lines never enter `GoodTOItems`/`BadTOItems`, the `SaveScan` filter (`:568`–`580`) is untouched.

5. **Mobile over-scan baseline fix:** the over-scan check (`TransferOrderItemView.razor.cs:482`) and remaining-qty calc (`:492`) currently use `NSLineQuantityPacked`, which is wrong on partially fulfilled orders. Replace with remaining qty (`LineQuantity − QuantityAlreadyShipped`).

### Partially shipped lines

A line shipped 150 of 200 stays in GOOD with remaining = 50; scan of 50 passes, 51 is rejected by the guard (Req 2.5, CP-5). Only fully-shipped lines migrate to DONE.

### Correctness properties

| ID | Requirement | Property |
|----|-------------|----------|
| CP-1 | 2.1 | A payload built from input containing a closed line contains no `orderLine` reference for it. |
| CP-2 | 2.2 | Scanned qty > remaining → exception naming line/item/qtys; zero HTTP calls made. |
| CP-3 | 2.3 | BAD bucket valid + GOOD bucket violating 2.2 → BAD not posted. |
| CP-4 | 2.4 | Closed lines never in `GoodTOItems`/`BadTOItems`; present in `DoneTOItems`. |
| CP-5 | 2.5 | Partial line (150/200 shipped) stays in GOOD; scan of 50 passes, 51 fails. |
| CP-6 | 2.7 | `DeduplicateFulfillmentLines` + `RequiresBin` outputs unchanged on existing fixtures. |

### Testing

- **Unit/integration:** extend `Integration.NS.Tests` (pattern: `NetSuiteApiClientServiceFulfillmentDedupeTests.cs`). Fixtures with closed and partially shipped lines; assert payload JSON (CP-1), guard exceptions (CP-2, CP-3), and preservation (CP-6).
- **Property-based testing:** NOT APPLICABLE — small, bounded line sets; conventional assertions give complete coverage.
- **Manual:** sandbox TO with mixed closed/open/partial lines — save succeeds or fails with the new message; device test for the DONE tab (Phase C).

---

## 4. Tasks

### Phase A — Data plumbing (the fix, part 1)

- **A1:** Add `tl.isclosed AS IsClosed`, `tl.quantityshiprecv AS QuantityAlreadyShipped` to `Integration.NS\NSScripts\NS_TO_x_Packing_Get_Items.sql`. No WHERE change. _Requirements: 2.1, 2.6_
- **A2:** Add `IsClosed`, `QuantityAlreadyShipped` to line DTOs; map through `StockTransferRequestPackingIntegration`, the STR packing handler, and `PostTransferOrderDTO`. _Requirements: 2.1_

### Phase B — Save guard (the fix, part 2)

- **B1:** In `SaveTOItemFulfillment` (`NetSuiteApiClientService.cs:1122`), after dedupe: filter closed lines; validate remaining qty per scanned line; throw actionable message before any NS call. _Requirements: 2.1, 2.2, 2.3_
- **B2:** Unit tests: closed-line exclusion (CP-1), remaining-qty rejection (CP-2, CP-3), preservation fixtures (CP-6). _Requirements: 2.1–2.3, 2.7_

**✅ Checkpoint:** `dotnet build` + `dotnet test` (Integration.NS.Tests) green; sandbox save on a partially fulfilled TO succeeds or fails with the new actionable message.

### Phase C — Mobile UX (the polish)

- **C1:** Map new fields into `TransferOrderLineVM` (both load paths, `TransferOrderItemView.razor.cs:84`–`180`); fix over-scan baseline to remaining qty (`:482`, `:492`). _Requirements: 2.2, 2.5_
- **C2:** Add `DoneTOItems` + DONE tab (badge count), route closed lines, exclude from `ItemRequest` and scan handlers; include DONE lines in `Refresh()` snapshot/restore (`:970`–`1022`). _Requirements: 2.4_
- **C3:** Manual device verification: closed lines read-only in DONE, partial line capped at remaining, save posts the open lines successfully. _Requirements: 2.4, 2.5, 2.7_

### Pre-retry operational step

Before the client retries on `ISTR-THM00100000631`, verify in NetSuite whether a prior BAD fulfillment already exists for the order (the BAD bucket posts before GOOD; a successful BAD followed by a failed GOOD leaves a committed fulfillment). If one exists, reconcile it before re-saving to avoid double-posting the BAD quantity.

---

## 5. Verification checklist

- [ ] SQL query returns new flags without changing row count (A1)
- [ ] `dotnet build` / `dotnet test` green after Phase B
- [ ] Sandbox: partially fulfilled TO saves successfully (only open lines posted)
- [ ] Sandbox: over-remaining scan → actionable message, no NS call (check logs)
- [ ] Mobile: DONE tab shows closed lines, read-only, badge count correct
- [ ] Mobile: `Refresh()` preserves scan state across all three tabs
- [ ] Existing dedupe/bin tests unchanged and passing

---

## 6. Accepted consequences & trade-offs

- Filtering upstream keeps the payload builder untouched, but every consumer of the packing query now carries two extra fields (negligible payload size).
- The BAD-before-GOOD split post remains a non-transactional window in this iteration; the new `QuantityAlreadyShipped` field enables the cheap mitigation below without restructuring.
- One fulfillment per save retains per-bucket failure isolation; a combined single GOOD+BAD post is structurally cleaner but deferred pending regression testing on bin-tracked locations.

---

## 7. Follow-up candidates (not authorized for implementation by this spec)

1. **Skip-already-fulfilled per bucket:** use `QuantityAlreadyShipped` in each bucket's guard to drop lines an earlier successful save already fulfilled — shrinks the retry risk using Phase A plumbing.
2. **Combined single GOOD+BAD post:** one transform call carrying mixed inventory statuses (the payload already resolves status per line via `InventoryStatusMap.Resolve`); eliminates the partial-post window structurally.
3. **Bucket-completion tracking:** record posted buckets so operator retries skip them entirely.
