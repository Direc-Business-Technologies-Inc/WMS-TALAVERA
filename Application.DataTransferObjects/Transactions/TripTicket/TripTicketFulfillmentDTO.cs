namespace Application.DataTransferObjects.Transactions.TripTicket;

public class TripTicketFulfillmentDTO
{
    public int NetsuiteTripTicketInternalId { get; set; }
    public int NetsuiteOrderInternalId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string DestinationLocation { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public DateTime NetsuiteOrderCreatedDate { get; set; }
}
