---
Date: 2026-10-06
Status: Confirmed
Mode: BUGFIX
Scope: Required, editable per-line Preferred Vendor on STR (TO/ITO/Return) and RTS lines; STR header vendor retained for Returns only
Related: None found (empty folder `.opencode/specs/iil-201-str-status-sort/` contains no documents)
---

# Bugfix Requirements Document

## Introduction

Preferred Vendor is captured at the STR document header and posted to NetSuite
only for return categories. The business requirement is per item line: each line
of every STR document type (TO, ITO, Return) and each RTS line carries a
REQUIRED, editable Preferred Vendor, defaulted from the NetSuite item vendor
master and posted back to `custcol_dbti_vendor` on that transaction only. The
STR header Preferred Vendor is retained (and remains required) for Return
categories only, because the Receiving module consumes it.

## Glossary

- **STR**: Stock Transfer Request — Blazor form creating transfer orders in NetSuite.
- **TO**: Transfer Order (`transferorder`, TransferCategory 1).
- **ITO**: Intercompany Transfer Order (`intercompanytransferorder`, TransferCategory 2).
- **Return**: STR return categories 3/4; posted as ITO.
- **RTS**: Return to Supplier — SupplierReturn module; posts a Vendor Return Authorization (VRA).
- **Preferred Vendor (master)**: vendor flagged `preferredvendor = 'T'` on an item's vendor list, subsidiary-scoped.
- **Line Vendor Field**: `custcol_dbti_vendor` (List/Record → Vendor) — confirmed
  present and populated on intercompany transfer order lines; TO/VRA coverage to
  be verified during implementation.
- **NS defaulting script**: NetSuite-side automation that fills
  `custcol_dbti_vendor` when empty on every save; NEVER overwrites an existing
  value (confirmed by NetSuite admin, 2026-10-06).
- **Owning subsidiary**: vendor-list scoping rule — intercompany → To Subsidiary,
  otherwise → source Subsidiary (`STRForm.razor.cs:221-228`).
- **Preservation**: existing behavior that must not regress.

## Bug Analysis

### Current Behavior (Defect)

1.1 WHEN a user creates, edits, or views any STR (TO, ITO, Return) or RTS line,
    THEN no per-line Preferred Vendor is displayed or captured.

1.2 WHEN an STR of category TO or ITO carries a header Preferred Vendor, THEN the
    vendor is silently dropped — the payload emits
    `custbody_dbti_return_to_vendor` only when `TransferCategory.IsReturn`
    (`StockTransferRequestIntegration.cs:376`) while the field is visible for all
    categories (`STRForm.razor:59`).

1.3 WHEN STR or RTS lines are read from or posted to NetSuite, THEN no per-line
    vendor data is included (`StockTransferRequestIntegration.cs:222-261,383-396`,
    `SupplierReturnIntegration.cs:100-129,412-421`).

### Expected Behavior (Correct)

2.1 WHEN a user edits an STR line in ANY category (TO, ITO, Return), THEN the
    line SHALL offer an editable Preferred Vendor dropdown of trade vendors
    scoped to the owning subsidiary, and the field SHALL be required.

2.2 WHEN a user edits an RTS line, THEN the line SHALL offer an editable
    Preferred Vendor dropdown scoped to the document subsidiary — including lines
    sourced from a Purchase Order — and the field SHALL be required.

2.3 WHEN a line is added (item picker or PO source), THEN its Preferred Vendor
    SHALL default to the item's Preferred Vendor (master) for the document
    subsidiary, and the user MAY override it per line. The NS defaulting script
    acts as a non-destructive server-side safety net for lines left empty.

2.4 WHEN an STR or RTS is created OR updated, THEN each line's Preferred Vendor
    SHALL be posted to `custcol_dbti_vendor`. Because both update endpoints
    replace the item sublist (`?replace=item`), the update payload SHALL include
    every line's current vendor so existing values are not wiped.

2.5 WHEN a Preferred Vendor is posted per line, THEN it SHALL be written only to
    `custcol_dbti_vendor` on that transaction; the item vendor master and all
    other transactions SHALL remain unaffected.

2.6 WHEN an existing STR or RTS is opened, THEN each line SHALL display the value
    stored in `custcol_dbti_vendor`. A line with no stored value SHALL display
    "Not set" in view mode; in edit mode the user SHALL be required to select a
    vendor before re-submitting (2.8).

2.7 WHEN an STR of category TO or ITO is displayed, THEN no header Preferred
    Vendor field SHALL be shown. WHEN an STR of a Return category is displayed,
    THEN the header Preferred Vendor SHALL be shown and SHALL remain required,
    and SHALL CONTINUE TO be posted as `custbody_dbti_return_to_vendor`.
    (CONFIRMED — Q3.)

2.8 WHEN any line has no Preferred Vendor selected, THEN the system SHALL block
    submission and SHALL name the offending line number(s). The dropdown SHALL
    NOT allow clearing a selected value; the required validation is the backstop.

2.9 RESOLVED — the Line Vendor Field is `custcol_dbti_vendor`, confirmed present
    and populated on ICTO lines via record inspection. The NS defaulting script
    is non-destructive (fills-if-empty), so user overrides persist. (Q4 CONFIRMED.)

### Unchanged Behavior (Preservation)

3.1 WHEN an RTS is created, THEN the system SHALL CONTINUE TO require and post
    the header vendor as the VRA `entity`.

3.2 WHEN an STR Return is submitted, THEN the system SHALL CONTINUE TO post
    `custbody_dbti_return_to_vendor`; the Receiving module's read of that field
    (`ReceivingIntegration.cs:288,320,379`) SHALL NOT be modified and SHALL
    CONTINUE TO work for new returns.

3.3 WHEN packing/receiving queries resolve vendor-assigned bins, THEN they SHALL
    CONTINUE TO use `itemvendor.preferredvendor` unchanged (all NSScripts files).

3.4 WHEN any other STR or RTS field, validation, approval, or posting behavior
    is exercised, THEN it SHALL behave exactly as before.

3.5 Documented architectural debts (`.claude/architectural_debts.md`) SHALL NOT
    be fixed or refactored as part of this work.

## Scope

### In-Scope
- STR lines grid: required, editable Preferred Vendor dropdown column (one form
  covers TO/ITO/Return).
- RTS lines grid: required, editable Preferred Vendor dropdown column (incl.
  PO-sourced lines).
- Line read queries + line NSDTO/DTO/VM additions (`VendorDTO?`/`VendorVM?` per
  line); Mapster wiring.
- Create + update payloads for STR and RTS: post `custcol_dbti_vendor` per line,
  with `?replace=item` wipe protection (2.4).
- Item picker default sourcing from `itemvendor.preferredvendor`
  (subsidiary-scoped, existing script precedent).
- Submit validation naming offending line numbers (2.8).
- STR header vendor: visibility + validation restricted to Return categories
  (fixes defect 1.2).

### Out-of-Scope
- NetSuite field creation (field exists and is populated, 2.9).
- Writing preferred vendor back to the item vendor master (prohibited, 2.5).
- Mobile (MAUI) views.
- Receiving module changes.
- Automated test projects (none exist; see Design testing strategy).

## Risks

- **R1**: RESOLVED — script ID confirmed as `custcol_dbti_vendor`.
- **R2**: The RTS create-from-PO path posts via NetSuite `!transform`; line fields
  are not set during transform. Accepted: the NS defaulting script fills empty
  line vendors on save (if it runs on VRA — verify during implementation);
  otherwise users set vendors on first edit.
- **R3**: Item-master default is scoped to the source subsidiary (existing script
  precedent). If the business expects destination-subsidiary vendors for
  ITO/Returns, 2.3 must be revised.
- **R4**: Documents created before this fix may have empty line vendors until
  next save; the NS script mitigates this on any subsequent save.
- **R5**: `custcol_dbti_created_vendor_returns` (List/Record → Transaction) is a
  DIFFERENT field with a different purpose; it must not be reused or modified.
  Verification pending that the NetSuite admin did not alter it.