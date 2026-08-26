using Application.DataTransferObjects.Transactions.Commons.NS;
using Shared.Libraries.ViewModel;

namespace Application.DataTransferObjects.Transactions.TripTicket.NS;

public class PostTripTicketDTO
{
    public int Parent { get; set; }

    public List<LocationDTO> Destinations { get; set; } = new();

    public DriverDTO Driver { get; set; } = new();

    public HelperDTO Helper { get; set; } = new();

    public TruckPlateNumberDTO TruckPlateNumber { get; set; } = new();

    public LocationDTO OriginLocation { get; set; } = new();

    public DateTime TripDate { get; set; }

    public string TruckSeal { get; set; } = string.Empty;

    public List<ItemFulfillmentDTO> ItemFulfillments { get; set; } = new();

    public List<SubsidiaryDTO> ToSubsidiaries { get; set; } = [];

    public SubsidiaryDTO? FromSubsidiary { get; set; }
}
