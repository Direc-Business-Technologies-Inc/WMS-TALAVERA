# Print Labels for Trip-Ticket-Exempt Returns and RTS Fulfillments

Date: 2026-10-05  
Status: Draft  
Mode: RCA  
Scope: Set NetSuite's Print Label field on trip-ticket-exempt Return and Returns-to-Supplier (VRA) IFs from Web and Mobile  
Related: [STR packing-label RCA](2026-10-05-packing-label-trip-ticket-exemption-rca.md)

## Summary

**Reported gap:** The label behavior was implemented for STR fulfillment, but the separate Return and RTS fulfillment paths did not include NetSuite's Print Label checkbox. The same behavior is required when these flows are submitted from either Web or Mobile. Mobile does not need a label list or print UI.

**Impact:** Before the fix, Return and VRA payloads contained `shipStatus` and item data but no `custbody_dbti_print_label` field. Their commands also passed client DTOs directly to the NetSuite service, so Mobile did not have a server-side guarantee that exemption state was populated.

## Terms

- **IF:** Item Fulfillment.
- **Return:** A Return transaction handled by the WMS Returns packing flow.
- **RTS:** Returns to Supplier, implemented through Vendor Return Authorization (VRA).
- **Trip-ticket exemption:** An active NetSuite record matching the transaction's source and destination locations.
- **Print Label field:** NetSuite field script ID `custbody_dbti_print_label`.

## Root Cause

The Return and VRA fulfillment payload classes previously declared `shipStatus` and `item`, but not `custbody_dbti_print_label` ([ReturnsIFPayloadDTO.cs](../../Application.DataTransferObjects/Transactions/Packing/NS/Payload/ReturnsIFPayloadDTO.cs), [VendorReturnAuthorizationIFPayloadDTO.cs](../../Application.DataTransferObjects/Transactions/Packing/NS/Payload/VendorReturnAuthorizationIFPayloadDTO.cs)). Consequently, those REST requests could not set the checkbox.

The existing NetSuite packing integrations already detect exemption by joining the active location-pair record, and Web handlers already pass the resulting state. However, the API commands did not independently resolve that state, so Mobile requests could reach the fulfillment service with the default `false` value ([ReturnPackingIntegration.cs](../../Integration.NS/Implementations/Transactions/Packing/ReturnPackingIntegration.cs#L74), [VendorReturnAuthorizationPackingIntegration.cs](../../Integration.NS/Implementations/Transactions/Packing/VendorReturnAuthorizationPackingIntegration.cs#L74), [ReturnsItemReceiptPackingHandler.cs](../../Web.BlazorServer/Handlers/Implementations/Transaction/Packing/Returns/ReturnsItemReceiptPackingHandler.cs#L116), [VendorReturnAuthorizationItemReceiptPackingHandler.cs](../../Web.BlazorServer/Handlers/Implementations/Transaction/Packing/VendorReturnAuthorization/VendorReturnAuthorizationItemReceiptPackingHandler.cs#L116)).

## Affected Paths and Blast Radius

- Web and Mobile Return scans call `PostReturnsIFCmd`; VRA scans call `PostVendorReturnAuthorizationIFCmd` through their API `SaveScan` endpoints.
- Both commands now resolve exemption through their existing packing integrations and set it on the submitted lines before calling NetSuite.
- `SaveReturnsItemFulfillment` and `SaveVRAItemFulfillment` now set `PrintLabel` from the server-resolved exemption state in each good/bad payload path. Their existing `shipStatus` behavior remains `C` for exempt and `B` otherwise ([NetSuiteApiClientService.cs](../../Integration.NS/Services/NetSuiteApiClientService.cs#L799)).
- The Print Label payload property is a C# `bool` serialized as JSON `true` for exempt transactions. NetSuite displays the checkbox as checked; SuiteQL reads it as `T`.
- The existing Web Packing Labels list filters checked IFs. Mobile label-list/printing UI and the separate Trip Ticket fulfillment query are not in scope.

## Corrective Direction

The corrective change adds `custbody_dbti_print_label` to both payload DTOs and sets it to the server-resolved exemption value for Return and VRA IF creation. Exemption is derived from the active source/destination pair, so Web and Mobile use the same authority. The existing ship-status and inventory assignment rules are preserved. The checkbox remains set after printing so reprints remain available.

## Validation Limits

- The Application, NetSuite integration, and API host projects were built after the changes; builds reported existing warnings but no compiler errors. Focused diagnostics reported no new issues in the changed command or payload files.
- No automated test project was found. No live NetSuite payload or physical Mobile-device transaction was exercised.
- Verify exempt and non-exempt Return and VRA IF creation through Web and Mobile against the updated API, inspect the NetSuite checkbox, and confirm the Web Packing Labels list includes checked IFs only.
- The external NetSuite print script is outside this repository; its effect on the checkbox after printing was not tested.
