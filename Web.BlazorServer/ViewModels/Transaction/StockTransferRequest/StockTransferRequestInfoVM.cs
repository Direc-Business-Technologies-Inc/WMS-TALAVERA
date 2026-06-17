using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.ViewModels.Transaction.StockTransferRequest;

public class StockTransferRequestInfoVM
{
    public string Status { get; set; } = string.Empty;
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public VendorVM? Vendor { get; set; } = null;
    public LocationVM? SourceLocation { get; set; } = null;
    public LocationVM? DestinationLocation { get; set; } = null;
    public SubsidiaryVM? Subsidiary { get; set; } = null;
    public SubsidiaryVM? ToSubsidiary { get; set; } = null;
    public string PreparedBy { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public Types Type { get; set; } = Types.TransferOrder;
    public DateTime Date { get; set; }
    public List<StockTransferRequestLineVM> Lines { get; set; } = [];

    public enum Types
    {
        TransferOrder,
        IntercompanyTransferOrder,
        Returns
    } 
}
