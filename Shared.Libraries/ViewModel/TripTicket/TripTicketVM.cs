using Shared.Libraries.ViewModel.ItemFulfillment;

namespace Shared.Libraries.ViewModel.TripTicket;

public class TripTicketVM
{
    public int Id { get; set; }
    public int Parent { get; set; }
    public string ParentName { get; set; }

    public List<LocationVM>? Destinations { get; set; } = new();

    public DriverVM? Driver { get; set; } = new();

    public HelperVM? Helper { get; set; } = new();

    public TruckPlateNumberVM? TruckPlateNumber { get; set; } = new();

    public LocationVM? OriginLocation { get; set; } = new();

    public DateTime? TripDate { get; set; }

    public string TruckSeal { get; set; } = string.Empty;

    public List<ItemFulfillmentVM> ItemFulfillments { get; set; } = new ();

    public List<SubsidiaryVM>? ToSubsidiaries { get; set; } = [];

    public SubsidiaryVM? FromSubsidiary { get; set; } = new();
}
