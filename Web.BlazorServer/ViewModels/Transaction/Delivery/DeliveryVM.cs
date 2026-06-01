using Web.BlazorServer.ViewModels.Transaction.Commons;

namespace Web.BlazorServer.ViewModels.Transaction.Delivery;

public class DeliveryVM : MarketingDocumentVM
{
    public DateTime PostingDate { get; set; }
    public DateTime DeliveryDate { get; set; }
    public DateTime DocumentDate { get; set; }

    public string? NumAtCard { get; set; } = null;
    public string? ContactPerson { get; set; } = null;
    public string? SchoolYear { get; set; } = null;
    public string? ActualDelivDate { get; set; } = null;
    public string? CourierName { get; set; } = null;
    public string? WayBillNo { get; set; } = null;
    public string? PlateNo { get; set; } = null;
    public string? DRNo { get; set; } = null;
    public string? DocRemarks { get; set; } = null;
    public string? Designation { get; set; } = null;
    public string? ReceivedBy { get; set; } = null;
    public string? ApprovedBy { get; set; } = null;
    public string? NotedBy { get; set; } = null;
    public string? PreparedBy { get; set; } = null;
    public string? DeliveryMeans { get; set; } = null;
    public string? Courier { get; set; } = null;
    public string? Driver { get; set; } = null;

    public IEnumerable<DeliveryLineVM> DocumentLines { get; set; } = [];
}
