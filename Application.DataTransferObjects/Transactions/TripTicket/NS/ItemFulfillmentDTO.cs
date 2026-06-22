namespace Application.DataTransferObjects.Transactions.TripTicket.NS;

public class ItemFulfillmentDTO
{
    public int NetsuiteOrderInternalId { get; set; }
    public string OrderNumber { get; set; }
    public string OrderStatus { get; set; }
    public string OrderType { get; set; }
    public DateTime NetsuiteOrderCreatedDate { get; set; }
}
