namespace Application.DataTransferObjects.Transactions.TripTicket;

public class TripTicketDataGridDTO
{
    public int NetsuiteTripTicketInternalId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string Driver { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime? TripDate { get; set; }
}
