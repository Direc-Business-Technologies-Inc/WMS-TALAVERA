namespace Web.BlazorServer.ViewModels.Transaction.Receiving;

public class ItemReceiptVM
{
    public enum SourceTypes
    {
        TransferOrder,
        PurchaseOrder,
        Returns
    }
    public SourceTypes SourceType { get; set; } = SourceTypes.PurchaseOrder;

    public string CreatedFrom { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;
    public string ReceivedBy { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;
    public string TransferLocation { get; set; } = string.Empty;
    public string Subsidiary { get; set; } = string.Empty;
    public string ToSubsidiary { get; set; } = string.Empty;

    public DateTime Date { get; set; }
    public bool IsBad { get; set; } = false;

    public int SourceInternalId { get; set; }
    public int DefaultBO { get; set; }
    public int VendorPrefferedBin { get; set; }
    public List<ItemReceiptLineVM> Lines { get; set; } = [];
}
