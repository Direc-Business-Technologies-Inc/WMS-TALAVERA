namespace Application.DataTransferObjects.Transactions.TripTicket;

public class TripTicketDataGridDTO
{
    public int NetsuiteTripTicketInternalId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string Driver { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Helper { get; set; } = string.Empty;
    public string HelperName { get; set; } = string.Empty;
    public int HelperId { get; set; } 
    public string TruckPlateNumber { get; set; } = string.Empty;
    public int TruckPlateNumberId { get; set; } 
    public DateTime? TripDate { get; set; }
}
