# Packing Labels for Trip-Ticket-Exempt STR Fulfillments

Date: 2026-10-05  
Status: Draft  
Mode: RCA  
Scope: Set the Print Label flag on trip-ticket-exempt STR item fulfillments in Web and Mobile, and filter the Web Packing Labels list by that flag  
Related: None found

## Summary

**Confirmed behavior:** Item fulfillments (IFs) created from trip-ticket-exempt Stock Transfer Requests (STRs) should have NetSuite's Print Label checkbox checked, whether created through Web or Mobile. Web's Packing Labels list should filter for IFs with that checkbox checked. Mobile does not need a label list or printing view.

**Impact:** The current STR fulfillment method references unresolved ship-status symbols, preventing compilation. Mobile's request model does not carry exemption state, so exemption must be resolved server-side for its fulfillment path.

This RCA is based on repository inspection. The reported production behavior and the external NetSuite print script were not tested.

## Terms

- **STR:** Stock Transfer Request.
- **IF:** Item Fulfillment created from a transfer order.
- **Trip-ticket exemption:** An active exemption record matching the transfer's source and destination locations.
- **Field script ID:** The NetSuite field identifier `custbody_dbti_print_label`.

## Root Cause

The IF payload correctly maps `custbody_dbti_print_label` to a C# `bool` ([TransferOrderIFPayloadDTO.cs](../../Application.DataTransferObjects/Transactions/Packing/NS/Payload/TransferOrderIFPayloadDTO.cs#L9)). At the inspected revision, `SaveTOItemFulfillment` references `GetNetSuiteShipStatus`, `status`, and `ItemFulfillmentShipStatus`, which are not defined in the repository; the method therefore does not compile. The preceding implementation set `shipStatus` to `C` for exempt STRs and `B` otherwise. Commit `b2bfc3c` added the print-label payload but replaced that valid mapping with the unresolved symbols. The flag must be derived from exemption without changing the established `C`/`B` ship-status behavior ([NetSuiteApiClientService.cs](../../Integration.NS/Services/NetSuiteApiClientService.cs#L757)).

Web obtains exemption state by matching an active source/destination location-pair record ([StockTransferRequestPackingIntegration.cs](../../Integration.NS/Implementations/Transactions/Packing/StockTransferRequestPackingIntegration.cs#L81)) and passes it into the fulfillment DTO ([ItemReceiptPackingHandler.cs](../../Web.BlazorServer/Handlers/Implementations/Transaction/Packing/ItemReceipt/ItemReceiptPackingHandler.cs#L125)).

Mobile posts `TransferOrderLineVM` values to the `SaveScan` endpoint ([TransferOrderItemView.razor.cs](../../Mobile.MAUI/Components/Pages/Packing/TransferOrder/TransferOrderItemView.razor.cs#L201), [TransferOrderController.cs](../../Api.CoreWebAPI/Controllers/Packing/TransferOrderController.cs#L40)). That view model does not include `IsTripTicketExempt`, so the current Mobile request shape cannot carry the exemption value.

## Affected Paths

- The shared NetSuite fulfillment creator handles STR IF creation from Web and Mobile.
- Web's Packing Labels query includes checkbox-checked IFs, but also includes status-`B` IFs regardless of checkbox ([ReturnPackingIntegration.cs](../../Integration.NS/Implementations/Transactions/Packing/ReturnPackingIntegration.cs#L193)). This is broader than the confirmed requirement to filter by the Print Label checkbox.
- Web already has a Packing Labels tab ([PackingPage.razor](../../Web.BlazorServer/Components/Pages/Transaction/Packing/PackingPage.razor#L31)). Mobile's packing page has STR, Returns, and RTS tabs ([PackingView.razor](../../Mobile.MAUI/Components/Pages/Packing/PackingView.razor#L9)); no Mobile label view is required.
- Another packed-fulfillment query also uses status `B` or checkbox checked and excludes fully received IFs ([NS_ItemFulfillment_Get_Packed.sql](../../Integration.NS/NSScripts/NS_ItemFulfillment_Get_Packed.sql#L17)). Its Trip Ticket usage should be preserved unless separately reviewed.

## Corrective Direction

1. Set Print Label to `true` for STR IFs when the STR is trip-ticket-exempt. The payload should serialize as JSON `true`, not the string `"T"`; `T` is the SuiteQL checkbox representation.
2. Resolve exemption from authoritative server-side data based on the active source/destination location-pair record so Web and Mobile apply the same rule. Mobile needs only this flag behavior; no Mobile label list or print controls are in scope.
3. Preserve the established ship-status mapping: `C` for exempt STRs and `B` otherwise.
4. Apply the same label decision to Good and Bad IF payloads, matching current shared behavior.
5. Filter the existing Web Packing Labels list by the Print Label checkbox. Preserve broader pending-IF filters used by other workflows, including the separate Trip Ticket fulfillment query.
6. Leave the Print Label checkbox checked after printing so the IF remains available for reprinting.

## Deferred Questions and Validation Limits

- The confirmed request concerns STR IFs. Extending the flag rule to Returns or Returns-to-Supplier IFs is out of scope.
- The external NetSuite print script is outside this repository; verify that it does not clear the checkbox after printing, because reprinting is allowed.
- No test project was found during repository inspection, and no live NetSuite payload was exercised. Verify the serialized payload and list visibility in the target NetSuite account before release.
