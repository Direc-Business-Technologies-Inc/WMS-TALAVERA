using Application.DataTransferObjects.Transactions.Commons.NS.Payload;
using Shared.Libraries.ViewModel.TripTicket;
using System.Text.Json.Serialization;
using static Application.DataTransferObjects.Transactions.TripTicket.NS.Payload.TripTicketPayloadDTO;

namespace Application.DataTransferObjects.Transactions.TripTicket.NS.Payload;

public class ItemFulfillmentStatusUpdatePayloadDTO
{
    [JsonPropertyName("shipStatus")]
    public ReferenceValue ShipStatus { get; set; }

    public static ItemFulfillmentStatusUpdatePayloadDTO ItemFulfillmentUpdateToShipped()
    {
        return new ItemFulfillmentStatusUpdatePayloadDTO
        {
            ShipStatus = new ReferenceValue
            {
                Id = "C"
            }
        };
    }
}
