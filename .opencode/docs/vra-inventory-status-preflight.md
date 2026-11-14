# Web VRA/RTS Inventory-Status Preflight

> Status: implemented
> Date: 2026-10-27
> Scope: Web packing fulfillment of Vendor Return Authorizations (RTS)

## Problem

The web RTS screen could submit a positive Good quantity with inventory status `1` even
when the selected source bin had no Good-status balance. The VRA query already returned
status-specific quantities, but the web posting handler discarded them. NetSuite was the
first contextual validator and returned an opaque `Invalid Field Value 1` HTTP 400.

The same condition can affect Bad status `3`.

## Fix

`VendorReturnAuthorizationItemReceiptPackingHandler` reloads source lines immediately
before posting. It now validates every selected positive Good and Bad quantity against
the status-specific balance from that fresh result.

The selected context matches payload construction:

| Location type | Availability checked |
|---|---|
| Non-bin | Item/location Good or Bad balance |
| Bin with vendor assignment | Item/vendor-bin Good or Bad balance |
| Bin without vendor assignment | Item/preferred-bin Good or Bad balance |

Quantities are converted from base units using the line UoM rate before comparison.

If any quantity exceeds the matching balance, the handler refuses the entire save before
dispatching `PostVendorReturnAuthorizationIFCmd`. The operator receives the offending
line, item, requested status, location/bin, requested quantity, and available quantity.
No partial Good or Bad fulfillment is created by this validation path.

## Files

- `Application.DataTransferObjects/Transactions/Packing/NS/VendorReturnAuthorizationInventoryAvailability.cs`
- `Web.BlazorServer/Handlers/Implementations/Transaction/Packing/VendorReturnAuthorization/VendorReturnAuthorizationItemReceiptPackingHandler.cs`
- `Integration.NS.Tests/Payloads/PackingIFPayloadDTOTests.cs`

## Limitations

- This guard covers the reported web flow. Mobile keeps its existing posting behavior.
- Availability can change between the SuiteQL reload and the REST transform. NetSuite
  remains the final concurrency authority.
- The guard does not substitute a different inventory status. Operators must classify
  the quantity correctly or correct the NetSuite item/location/bin balance.
- An account-level status configuration mismatch can still produce a NetSuite rejection
  even when inventory balance exists.

## Verification

Run:

```powershell
dotnet test Integration.NS.Tests/Integration.NS.Tests.csproj --filter "FullyQualifiedName~PackingIFPayloadDTOTests"
dotnet build Web.BlazorServer/Web.BlazorServer.csproj --no-restore
```
