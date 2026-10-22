# Requirements — STR packing line read: completeness, vendor scope, gate stability

**Date:** 2026-10-22
**Status:** Confirmed
**Mode:** BUGFIX (defect fix + hardening)
**Scope:** `Integration.NS/NSScripts/NS_TO_x_Packing_Get_Items.sql`, `Application.UseCases` packing Transfer-Order queries, `Api.CoreWebAPI` packing controller, `Mobile.MAUI` Packing → Transfer Order screen
**Related:**
- `.opencode/specs/2026-10-06-preferred-vendor-per-line/` — introduced the per-line vendor field this gate filters on.
- `.opencode/specs/2026-10-16-to-partial-fulfillment-if-fix/plan.md:77` — prior decision that the script does not filter rows by state; reconciled in `design.md` (D9).
- `.opencode/docs/changes/2026-10-21-mobile-str-vendor-gated-fulfillment.md` — the change under review; its save-path rationale is corrected by requirement 5.1.

## Introduction

The mobile Packing → Transfer Order screen reads one SuiteQL script
(`NS_TO_x_Packing_Get_Items`) to obtain an STR's SHIPPING lines. That read is
subject to NetSuite's 1,000-row SuiteQL page limit, which the client does not
honour, and one family of joins in the script duplicates rows. On a real
document with **1,657 SHIPPING lines** the mobile app receives at most 1,000
rows, and the rows it does receive include **4 duplicates** (1,661 joined rows
vs 1,657 distinct lines, measured 2026-10-22).

The vendor gate added on 2026-10-21 reads the same script twice per vendor scan
and filters in memory. On a document with **95 distinct vendors** that means
repeatedly transferring ~1,657 rows to use ~17 of them.

This specification stops the silent data loss, makes the row set structurally
exact, and moves the vendor scope into SQL so the gate is both correct and fast.

## Glossary

- **STR / TO** — the Stock Transfer Request and the NetSuite transfer order it becomes. One document, many lines.
- **SHIPPING line** — `transactionline.transactionlinetype = 'SHIPPING'`; the only line type this read consumes.
- **Line vendor** — `transactionline.custcol_dbti_vendor`; the vendor the line was requested from, printed under the Vendor field, and the gate's filter key.
- **Vendor gate** — the handheld's mandatory vendor-barcode scan before any line is listed.
- **SuiteQL page** — one HTTP response from NetSuite's SuiteQL endpoint, carrying at most 1,000 rows plus `hasMore` / `totalResults`.
- **Fan-out** — one source line producing multiple result rows because a joined table is not pre-aggregated.
- **Vendor scope** — an optional per-call restriction of the line read to one line vendor.
- **Sentinel `0`** — the vendor-scope value meaning "no scope"; every real vendor id is `> 0`.
- **IF** — the NetSuite Item Fulfillment record the save posts.

## Bug conditions and current behaviour

| Item | Detail |
|---|---|
| Trigger A | An STR whose SHIPPING-line count exceeds 1,000 |
| Observed A | Sample order has **1,657** SHIPPING lines (SuiteQL count, 2026-10-22). The read returns at most 1,000. |
| Trigger B | A line whose item has more than one matching `inventorybalance` row |
| Observed B | **1,661 joined rows for 1,657 distinct lines** — 4 duplicate lines (SuiteQL, 2026-10-22) |
| Expected | Every SHIPPING line, each exactly once |
| Evidence A | `NS_TO_x_Packing_Get_Items.sql` has no `LIMIT` and no `ORDER BY`; `NetSuiteApiClientService.cs:895-898` adds no paging parameters; `:916-918` discards `hasMore`. `NetSuiteResponse<T>` already models `hasMore` / `totalResults` (`NetsuiteTransactionApiDTO.cs:24-30`). |
| Evidence B | Four `LEFT JOIN inventorybalance` clauses at `sql:118-142` are not pre-aggregated, while the analogous `ibli1` / `ibli2` joins at `:144-186` are `SUM`-grouped. |
| Impact | Mobile packing list missing lines; mobile save payload built from an incomplete set; web packing `ItemReceiptPackingHandler` (`:19,43`) reads the same truncated set; vendor candidates drawn from the truncated set, so a vendor beyond row 1,000 is reported as not on the order. |
| Existing mitigation | `DeduplicateFulfillmentLines` (`NetSuiteApiClientService.cs:1698`) dedupes the save payload, so duplicate rows do **not** corrupt the post. The displayed list and the scan handlers are not protected. |

## Requirements

### 1. Line read completeness

**1.1** WHEN an STR has more than 1,000 SHIPPING lines THEN the line read SHALL return every SHIPPING line for that tranid.

**1.2** WHEN the read is split across more than one SuiteQL page THEN it SHALL use a total, stable row ordering so consecutive pages neither duplicate nor omit a line.

**1.3** WHEN the script's joins would produce more than one row for a line THEN the read SHALL return that line exactly once.

**1.4** WHEN the read changes THEN every existing caller (mobile list, mobile save-payload construction, web packing, vendor resolver) SHALL receive the same or a superset of the rows it receives today.

**1.5** WHEN an STR has 1,000 or fewer SHIPPING lines THEN the read SHALL issue no additional NetSuite request beyond the first.

**1.6** WHEN a vendor scope is supplied THEN the restriction SHALL be applied by the query sent to NetSuite, so only that vendor's lines are transferred.

**1.7** WHEN no vendor scope is supplied THEN the read SHALL return all lines, unchanged from today's row set.

**1.8** WHEN any page of a multi-page read fails THEN the read SHALL fail as a whole; a partial row set SHALL NOT be returned as success.

**1.9** WHEN the rows delivered across the pages account for fewer lines than the response's `totalResults` THEN the read SHALL fail rather than return the short set.

### 2. Vendor resolution

**2.1** WHEN resolving a scanned vendor barcode THEN candidate vendors SHALL be drawn from the complete set of vendors on the document, independent of any vendor's row position in the line read.

**2.2** WHEN resolving a vendor barcode THEN resolution SHALL NOT perform a second full line read of the document.

**2.3** WHEN an STR has no vendor on any line THEN the failure SHALL name an actionable alternative route for the operator.

**2.4** WHEN resolution fails because of an infrastructure fault THEN the API SHALL answer with a readable message rather than an unhandled 500.

**2.5** WHEN a SHIPPING line carries no vendor THEN the candidate read SHALL NOT emit a row for it, so no `NULL` or non-positive id reaches the vendor-record read.

**2.6** WHEN the candidate read lists vendors THEN its document-level filters SHALL mirror the line read's (`recordtype`, transfer category, `status`), so a resolved vendor's lines are always visible to the scoped line read.

### 3. Mobile vendor-gate behaviour

**3.1** WHEN a vendor scan is submitted THEN the screen SHALL indicate that resolution is in progress until it completes.

**3.2** WHEN the vendor is resolved but lines are still loading THEN the screen SHALL display the resolved vendor before the lines appear.

**3.3** WHEN the operator changes vendor while unsaved scans exist THEN the screen SHALL offer Save now / Discard / Cancel.

**3.4** WHEN the operator chooses Save now THEN the vendor switch SHALL occur only after the save succeeds.

**3.5** WHEN the save fails THEN the current vendor and its unsaved scans SHALL be retained.

### 4. Preservation

**4.1** The save payload and `SaveTOItemFulfillment` semantics SHALL remain unchanged.

**4.2** The web packing flow SHALL remain ungated and SHALL continue to fulfil the whole order.

**4.3** Receiving, trip ticket, returns, and VRA paths SHALL remain unaffected.

**4.4** Documents with no vendor on their lines SHALL remain out of scope for handheld fulfilment.

**4.5** WHEN hardening lands THEN "vendor-scan to lines-visible" SHALL NOT be slower than "page-open to lines-visible" was before the gate, measured on a document of at least 1,000 lines.

**4.6** WHEN the complete line set reaches web packing THEN the growth of the web save payload (`PostItemFulfillment` posts zero-quantity entries for every non-completed line) SHALL be verified against NetSuite's limits or recorded as an accepted risk.

### 5. Documentation accuracy

**5.1** The change document's save-path rationale SHALL describe mobile's actual behaviour: only scanned lines are posted.

**5.2** The vendor barcode payload SHALL be documented as the vendor name, not the NetSuite internal id.

**5.3** The reason the vendor-resolution busy flag must span the follow-up item load SHALL be recorded at the flag.

## Out of scope

- Timing records and text-file export of request/response data (deferred at the user's request).
- Determinate progress bar (spinner retained per colleague requirement).
- In-memory retention of scanned lines across vendor switches.
- Ungated fallback for null-vendor documents (accepted risk).
- Merged multi-vendor save payloads.
- NetSuite print scriptlet changes.
- Any database schema change.

## Preservation of prior decisions

`2026-10-16-to-partial-fulfillment-if-fix/plan.md:77` decided that the script does
not filter rows by state, so each consumer decides. Requirement 1.6 adds an
optional call-scoped vendor predicate whose default (`0`) reproduces today's row
set exactly; it does not filter by state. `design.md` (D9) records the
reconciliation explicitly.
