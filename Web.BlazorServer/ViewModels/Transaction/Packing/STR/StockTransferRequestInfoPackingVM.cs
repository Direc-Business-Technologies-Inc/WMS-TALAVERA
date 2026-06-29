using Application.DataTransferObjects.Transactions.Packing.STR;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.ViewModels.Transaction.Packing.STR;

public class StockTransferRequestInfoPackingVM
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public VendorVM? Vendor { get; set; } = null;
    public LocationVM? SourceLocation { get; set; } = null;
    public LocationVM? DestinationLocation { get; set; } = null;
    public SubsidiaryVM? Subsidiary { get; set; } = null;
    public SubsidiaryVM? ToSubsidiary { get; set; } = null;
    public TransferOrderStatusPackingVM Status { get; set; } = new();
    public string PreparedBy { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public List<StockTransferRequestLinePackingVM> Lines { get; set; } = [];
    public TransferCategoryPacking Category { get; set; } = TransferCategoryPacking.Transfer;
    public bool IsReturn => Category.IsReturn;
    public bool IsIntercompany => Category.IsInterCompany;
}
