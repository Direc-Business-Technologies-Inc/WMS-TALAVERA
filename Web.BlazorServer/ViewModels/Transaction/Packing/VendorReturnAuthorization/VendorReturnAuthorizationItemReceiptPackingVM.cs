namespace Web.BlazorServer.ViewModels.Transaction.Packing.VendorReturnAuthorization;

public class VendorReturnAuthorizationItemReceiptPackingVM
{
    public enum SourceTypes
    {
        TransferOrder,
        PurchaseOrder,
        Returns,
        VendorReturnAuthorization
    }

    public SourceTypes SourceType { get; set; } = SourceTypes.VendorReturnAuthorization;

    public string CreatedFrom { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;
    public string ReceivedBy { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string TransferLocation { get; set; } = string.Empty;
    public string Subsidiary { get; set; } = string.Empty;
    public string ToSubsidiary { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int SourceInternalId { get; set; }
    public int DefaultBO { get; set; }
    public int VendorPrefferedBin { get; set; }
    public List<VendorReturnAuthorizationItemReceiptLinePackingVM> Lines { get; set; } = [];
}
