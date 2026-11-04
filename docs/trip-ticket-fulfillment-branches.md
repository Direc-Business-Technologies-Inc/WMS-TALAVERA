# Trip Ticket Fulfillment Branches

## Requirement

When creating or editing a trip ticket in WMS Web, users need to identify each Item Fulfillment (IF) by its source and destination branch. Show both locations and both subsidiaries in the fulfillment selection dialog and in the trip ticket fulfillment section.

## Displayed values

| UI column | Data source |
| --- | --- |
| From Location | Item Fulfillment transaction line location (`transactionline.location`) |
| To Location | Item Fulfillment transfer location (`transaction.transferlocation`) |
| From Subsidiary | Item Fulfillment subsidiary (`transaction.subsidiary`) |
| To Subsidiary | Item Fulfillment destination subsidiary (`transaction.tosubsidiary`) |

Location and subsidiary internal IDs are also included in the fulfillment DTO/view model alongside their display names. The selection and saved-trip-ticket fulfillment queries return the same branch metadata so the columns remain available after saving and when viewing or editing a trip ticket.

## Implementation locations

- `Integration.NS/Implementations/Transactions/TripTicketIntegration.cs` selects source/destination branch metadata from NetSuite for the available fulfillment list and the trip-ticket fulfillment queries.
- `Application.DataTransferObjects/Transactions/ItemFulfillment/ItemFulfillmentDTO.cs` and `Application.DataTransferObjects/Transactions/TripTicket/TripTicketFulfillmentDTO.cs` carry NetSuite branch data through the query boundary.
- `Shared.Libraries/ViewModel/ItemFulfillment/ItemFulfillmentVM.cs` exposes that data to the web UI.
- `Web.BlazorServer/Components/Pages/Transaction/TripTicket/Component/TripTicketFulfillmentSelection.razor` and `Web.BlazorServer/Components/Pages/Transaction/TripTicket/TripTicketCVU.razor` display the four columns.

The source location comes from the main transaction line; the destination location comes from the transaction transfer location. The query scopes the transaction-line join to its mainline row to keep one result per fulfillment.
