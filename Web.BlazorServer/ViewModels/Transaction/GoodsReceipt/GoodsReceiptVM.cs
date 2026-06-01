using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.Transaction.Commons;
using Web.BlazorServer.ViewModels.Transaction.GoodsIssue;

namespace Web.BlazorServer.ViewModels.Transaction.GoodsReceipt;

public class GoodsReceiptVM : MarketingDocumentVM
{
    public string PreparedBy { get; set; } = string.Empty;
    public WarehouseVM Warehouse { get; set; } = new();
    public TransactionTypeVM TransactionType { get; set; } = new();
    public new BusinessPartnerVM? BusinessPartner { get; set; } = null;
    public string? WarNo { get; set; } = null;
    public string? PurNo { get; set; } = null;
    public string? DocRemarks { get; set; } = null;
    public string? Designation { get; set; } = null;
    public string? ReceivedBy { get; set; } = null;
    public string? ApprovedBy { get; set; } = null;
    public string? NotedBy { get; set; } = null;
    public IEnumerable<GoodsReceiptLineVM> DocumentLines { get; set; } = [];
}
