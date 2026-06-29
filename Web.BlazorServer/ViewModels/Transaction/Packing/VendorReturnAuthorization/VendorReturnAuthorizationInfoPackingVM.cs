namespace Web.BlazorServer.ViewModels.Transaction.Packing.VendorReturnAuthorization;

public class VendorReturnAuthorizationInfoPackingVM
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string FromSubsidiary { get; set; } = string.Empty;
    public string ToSubsidiary { get; set; } = string.Empty;
    public string SourceWarehouse { get; set; } = string.Empty;
    public string DestinationWarehouse { get; set; } = string.Empty;
    public string PreparedBy { get; set; } = string.Empty;
    public string ReceivedBy { get; set; } = string.Empty;
}
