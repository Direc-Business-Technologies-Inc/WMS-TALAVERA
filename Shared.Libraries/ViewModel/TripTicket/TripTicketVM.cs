using Shared.Libraries.ViewModel.ItemFulfillment;

namespace Shared.Libraries.ViewModel.TripTicket;

public class TripTicketVM
{
    public int Id { get; set; }

    public List<LocationVM> Destinations { get; set; } = [];

    public DriverVM? Driver { get; set; }

    public HelperVM? Helper { get; set; }

    public TruckPlateNumberVM? TruckPlateNumber { get; set; }

    public LocationVM? OriginLocation { get; set; }

    public DateTime? TripDate { get; set; }

    public string TruckSeal { get; set; } = string.Empty;

    public List<ItemFulfillmentVM> ItemFulfillments { get; set; } = [];
}
