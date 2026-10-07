# RCA: Preferred Vendor Capture Gap in STR/RTS and Side Effects of the Line-Level Fix

**Date:** 2026-10-06
**Status:** Confirmed
**Mode:** RCA (analysis only — no fix authorized by this document)
**Scope:** Stock Transfer Request (all tabs) and Return to Supplier — Preferred Vendor capture, commit `6acb4f91d727db419738e52ff62a726579510fbc`
**Related:** None found

## 1. Symptom and Impact

**Original defect (pre-fix):**

- Preferred Vendor existed only as a header-level concept (`custbody_dbti_return_to_vendor`). The header UI field was commented out of `STRForm.razor`, and the outbound NetSuite payload did not send the header field.
- Line-level vendor (`custcol_dbti_vendor`) was never populated by WMS-created transactions. `StockTransferRequestLineVM.Vendor` was a non-nullable `VendorVM = new()` that was never assigned — a placeholder, not a feature.
- **Impact:** for Return documents, users had no way to capture which vendor items return to; per-line vendor assignment (required by the business for all STR tabs and RTS) was impossible; downstream pages that display the header vendor for returns (`ReceivingIntegration.cs:381` CASE expression) depended on a field WMS did not reliably send.

**Side effects of the fix (post-`6acb4f9`, from code review):**

- Item selection grids in **Inventory Adjustment** and **Inventory Transfer Request** can now return **duplicate item rows** for multi-subsidiary items.
- Return-document payload behavior changed (header vendor field re-enabled) without recorded verification.

## 2. Evidence-Backed Root Cause

**Root cause: incomplete feature design — vendor capture was modeled header-only, then the header UI was disabled without a line-level replacement.**

Mechanism, with evidence:

1. **Header-only data model.** `StockTransferRequestInfoVM.Vendor` existed; the line VM's `Vendor` was initialized to `new()` and never populated anywhere (verified in pre-fix `StockTransferRequestLineVM.cs`). The payload builder never emitted `custcol_dbti_vendor`.
2. **Capture path severed.** Commit `d72d268` commented out both the header vendor payload line *and* the header vendor UI field. Whatever the intent (the commit message is about subsidiary filtering, suggesting the commenting was incidental), the result was that **returns had zero vendor capture** — neither header nor line.
3. **No default source.** The shared item query (`GetItemsByLocationDataGridAsync`) had no join to `itemvendor`, so the item master's preferred vendor was unavailable to default anything.

**Contributing factor:** the item query is shared by four modules (STR, RTS, ITR, Inventory Adjustment) with no per-consumer opt-in, so any column added to it propagates to every consumer — the mechanism behind side effect S1 below.

**Labeled hypothesis (not verifiable from the repo):** the exact NetSuite-side reason `custbody_dbti_return_to_vendor` was commented out in `d72d268` is unknown; SuiteQL/REST behavior against `itemvendor` and `BUILTIN.DF` cannot be confirmed from source alone.

## 3. The Fix Mechanism (commit `6acb4f9`)

- New `SuiteQLFragments.PreferredVendor(subsidiary)` subquery joins `itemvendor` (where `preferredvendor = 'T'`, optionally subsidiary-scoped) into the location item query → exposes `PreferredVendorId/Name` on `ItemsDTO/ItemsVM`.
- Line VMs/DTOs/NSDTOs gained a **nullable** `Vendor`; all line-creation paths (item dialog, barcode scan, UoM split, RTS add/scan) default it from the item master (RTS: header vendor first, item master fallback).
- Payloads post `custcol_dbti_vendor` per line (create **and** update, STR and RTS); header `custbody_dbti_return_to_vendor` re-enabled for Return categories; submit guards enforce per-line and return-header vendor.

## 4. Affected Paths and Blast Radius

| ID | Area | Effect | Confidence |
|----|------|--------|------------|
| S1 | `SuiteQLFragments.PreferredVendor(null)` + `ItemsIntegration.cs:89` join | Unscoped join duplicates multi-subsidiary items in **Inventory Adjustment** (`ItemSelection.razor:6`) and **ITR** (`ITRForm.razor:163`), which never pass `SubsidiaryId`; also STR/RTS dialogs before a subsidiary is chosen. Inflates rows and counts. Regression vs. pre-fix behavior. | High (code-verified mechanism) |
| S2 | `StockTransferRequestIntegration.cs:379` | `custbody_dbti_return_to_vendor` re-enabled after being deliberately commented in `d72d268`. If NS rejects it, return create/update breaks. | Hypothesis — requires live NS test |
| S3 | `SupplierReturnForm.razor.cs:124-127` | RTS line vendor dropdown uses `GetVendorsListAsync` (all vendors, unscoped) vs. STR's trade-vendors-by-subsidiary — cross-subsidiary/non-trade vendors selectable. | High |
| S4 | `SupplierReturnIntegration.cs:238-259` | PO-transform RTS path omits `custcol_dbti_vendor` — no default on that path; submit guard catches it late. | High |
| S5 | `STRForm.razor.cs:596-599` | To-Subsidiary change silently nulls all line vendors (no confirm/toast). | High |
| S6 | STR Return tab | Header vendor **and** every line vendor required; lines default from item master, not header vendor (RTS does header-first) — double entry, inconsistent. | High |

## 5. Relevant History (verified via git)

- `d6d8750` — added `custbody_dbti_return_to_vendor` to the return payload (intent: capture return vendor at header).
- `d72d268` — commented out that line (and purchase category/subcategory; the latter two were since re-enabled — active at `StockTransferRequestIntegration.cs:380-381` today).
- `6acb4f9` — line-level vendor support end-to-end; re-enabled the header field for returns.

## 6. Recommended Corrective Direction (not an authorized plan)

1. **S1 (priority):** skip the `PreferredVendor` join when no subsidiary is scoped, or pass `SubsidiaryId` from ITR/Inventory Adjustment — an unscoped "preferred vendor" is ambiguous anyway.
2. **S2:** live-test NS create/update of a Return document with `custbody_dbti_return_to_vendor` before release.
3. **S3:** scope the RTS line vendor provider to trade vendors by subsidiary, matching STR.
4. **S4/S5/S6:** extend the PO-transform payload; add a warning toast on subsidiary-change vendor clearing; consider defaulting STR return lines from the header vendor.

## 7. Deliberately Deferred Concerns

- **Automated regression coverage:** no test projects exist in `WMS.slnx`; introducing test infrastructure is out of scope for this analysis.
- **NetSuite-side semantics** (`itemvendor.subsidiary` column behavior, `BUILTIN.DF` on joined aliases, multi-preferred-vendor data anomalies): not verifiable from this repo — flagged as hypotheses.
- **`.claude/` guidance debts:** referenced by CLAUDE.md but absent from the repo; no architectural-debt register was consulted.
- **Header-vs-line vendor end-state for returns** (whether the business wants the header field removed entirely once lines carry vendors): a business decision, deferred.

## Validation Limitation

Evidence comes from git history and source reads only; no build or test execution was performed in the analysis session (tooling restricted to git read commands).
