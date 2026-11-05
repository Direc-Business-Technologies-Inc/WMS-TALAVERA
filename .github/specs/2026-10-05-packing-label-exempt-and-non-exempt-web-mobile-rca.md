# Packing Labels for Exempt and Non-Exempt Fulfillments

Date: 2026-10-05  
Status: Confirmed  
Mode: RCA  
Scope: Print Label eligibility for STR, Returns, and RTS item fulfillments created through Web and Mobile  
Related: [STR packing-label RCA](2026-10-05-packing-label-trip-ticket-exemption-rca.md); [Returns and RTS packing-label RCA](2026-10-05-packing-label-exemption-returns-rts-rca.md)

## 1. Summary

The current changes set NetSuite's Print Label checkbox only when the source transaction is trip-ticket-exempt. At the same time, the Web Packing Labels query now includes only IFs whose Print Label checkbox is checked.

Together, these changes exclude newly created non-exempt IFs from the Web Packing Labels list.

The clarified behavior is that both exempt and non-exempt IFs should receive packing-label eligibility, regardless of whether fulfillment is submitted through Web or Mobile.

Mobile requires only the correct server-side posting behavior. No Mobile Packing Labels tab, print screen, or additional view is required.

## 2. Terms

- **IF:** Item Fulfillment.
- **STR:** Stock Transfer Request, fulfilled through the transfer-order flow.
- **Returns:** Transactions handled by the WMS Returns packing flow.
- **RTS:** Returns to Supplier, implemented through Vendor Return Authorization.
- **Trip-ticket exemption:** An active exemption record matching the source and destination locations.
- **Print Label:** NetSuite checkbox identified by `custbody_dbti_print_label`. REST payloads use JSON `true`/`false`; SuiteQL represents checked values as `'T'`.

## 3. Symptom and Impact

### Current behavior

| Source transaction | IF status | Posted Print Label | Web label visibility |
|---|---|---|---|
| Exempt | Shipped (`C`) | `true` | Included |
| Non-exempt | Packed (`B`) | `false` | Excluded |

This affects STR, Returns, and RTS fulfillments submitted through either client.

Previously, the Web label query admitted Packed IFs even when Print Label was unchecked. Removing that fallback exposes the non-exempt label-eligibility gap.

### Expected behavior

| Source transaction | IF status to preserve | Posted Print Label | Web label visibility |
|---|---|---|---|
| Exempt | Shipped (`C`) | `true` | Included |
| Non-exempt | Packed (`B`) | `true` | Included |

Exemption should determine shipping status, not whether the IF can receive a packing label.

## 4. Evidence-Backed Root Cause

### 4.1 Label eligibility is coupled to exemption

All three fulfillment methods currently derive the label flag from exemption:

```csharp
var itemFulfillmentStatus = isTripTicketExempt ? "C" : "B";
var printLabel = isTripTicketExempt;
```

Consequently, non-exempt transactions explicitly post Print Label as false.

Evidence: [NetSuiteApiClientService.cs](../../Integration.NS/Services/NetSuiteApiClientService.cs#L752), methods:

- `SaveTOItemFulfillment`
- `SaveReturnsItemFulfillment`
- `SaveVRAItemFulfillment`

### 4.2 The Web query no longer includes unchecked Packed IFs

The current query requires:

```csharp
Equal("t.custbody_dbti_print_label", "T")
```

The previous condition was Packed status or Print Label checked.

Evidence: [ReturnPackingIntegration.cs](../../Integration.NS/Implementations/Transactions/Packing/ReturnPackingIntegration.cs#L176).

### 4.3 Both clients converge on shared posting logic

Web packing handlers dispatch the fulfillment commands. Mobile's existing `SaveScan` endpoints dispatch the same commands, which call the shared NetSuite fulfillment methods.

The defect therefore belongs in the shared posting decision, not in separate Web or Mobile UI controls.

## 5. Affected Paths and Blast Radius

- STR fulfillment, including good and bad item payloads.
- Returns fulfillment payloads.
- RTS fulfillment, including good and bad item payloads.
- Web Packing Labels visibility for IFs created through either client.
- NetSuite Print Label values on newly created IFs.

Existing non-exempt IFs with Print Label unchecked will remain excluded unless separately corrected. Changing creation behavior does not retroactively update records.

## 6. Recommended Corrective Direction

1. Decouple Print Label from trip-ticket exemption.
2. Set `custbody_dbti_print_label = true` for every IF created through the scoped STR, Returns, and RTS packing flows.
3. Apply that rule consistently to every generated payload, including good and bad item splits.
4. Preserve exemption-driven status:
   - Exempt -> Shipped (`C`).
   - Non-exempt -> Packed (`B`).
5. Preserve authoritative server-side exemption resolution; it remains necessary for status selection.
6. Keep the Web label list checkbox-driven. Do not restore the Packed-status fallback.
7. Keep Mobile's existing submission UI and request flow unchanged.
8. Preserve inventory quantities, bins, inventory statuses, and existing error handling.
9. Preserve label availability for reprinting; verify the external print script does not clear the checkbox.

These are recommendations only. This RCA does not authorize implementation.

## 7. Validation Criteria

Test all 12 core combinations:

```text
Web / Mobile
x STR / Returns / RTS
x Exempt / Non-exempt
```

For each successfully created IF, verify:

- NetSuite Print Label is checked.
- Status remains `C` for exempt or `B` for non-exempt.
- The IF appears in the Web Packing Labels tab after refresh.
- Items and quantities match the submission.

Additional checks:

- Verify good and bad payload paths for STR and RTS.
- Confirm unchecked IFs remain excluded regardless of exemption or Packed status.
- Confirm checked IFs from Mobile appear in Web Packing Labels without adding Mobile views.
- Verify printing and reprinting independently.
- Confirm RTS source/destination data supports the existing exemption lookup.

## 8. Deferred Concerns and Validation Limits

- Historical IF backfill requires a separate decision and controlled process.
- External NetSuite workflows may override the checkbox; live verification is required.
- Changes to Trip Ticket selection rules are outside scope. Its separate query also references Print Label, so regression-test selection behavior without changing that query.
- No SAP, database-schema, authorization, or architectural-debt changes are proposed.
- Some repository guides describe an older LSMS/SAP architecture; this RCA follows the verified WMS NetSuite paths.
- No automated test project was found. No builds, live NetSuite postings, or device tests were performed for this RCA.
