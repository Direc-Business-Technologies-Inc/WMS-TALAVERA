namespace Application.DataTransferObjects.Transactions.TripTicket;

public class TripTicketDataGridDTO
{
    public int Parent { get; set; }
    public string ParentName { get; set; } = string.Empty;
    public int NetsuiteTripTicketInternalId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ToSubsidiaryIds { get; set; } = string.Empty;
    public string ToSubsidiary { get; set; } = string.Empty;
    public int FromSubsidiaryId { get; set; }
    public string FromSubsidiary { get; set; } = string.Empty;
    public string DestinationIds { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public int DriverId { get; set; }
    public string Driver { get; set; } = string.Empty;
    public int LocationId { get; set; }
    public string Location { get; set; } = string.Empty;
    public int HelperId { get; set; } 
    public string Helper { get; set; } = string.Empty;
    public string HelperName { get; set; } = string.Empty;
    public string TruckPlateNumber { get; set; } = string.Empty;
    public int TruckPlateNumberId { get; set; } 
    public DateTime? TripDate { get; set; }
    public string TruckSeal { get; set; } = string.Empty;
}
