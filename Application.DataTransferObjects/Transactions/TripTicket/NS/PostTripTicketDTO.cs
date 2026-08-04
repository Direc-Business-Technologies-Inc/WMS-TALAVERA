using Application.DataTransferObjects.Transactions.Commons.NS;

namespace Application.DataTransferObjects.Transactions.TripTicket.NS;

public class PostTripTicketDTO
{
    public List<LocationDTO> Destinations { get; set; } = new();

    public DriverDTO Driver { get; set; } = new();

    public HelperDTO Helper { get; set; } = new();

    public TruckPlateNumberDTO TruckPlateNumber { get; set; } = new();

    public LocationDTO OriginLocation { get; set; } = new();

    public DateTime TripDate { get; set; }

    public string TruckSeal { get; set; } = string.Empty;

    public List<ItemFulfillmentDTO> ItemFulfillments { get; set; } = new();
}
