using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.Transaction.Commons;

namespace Web.BlazorServer.ViewModels.Transaction.GoodsIssue;

public class GoodsIssueVM : MarketingDocumentVM
{
    public string? SchoolYear { get; set; } = null;
    public TransactionTypeVM? TransactionType { get; set; } = null;
    public new BusinessPartnerVM? BusinessPartner { get; set; } = null;
    public string? SrfNo { get; set; } = null;
    public string? Designation { get; set; } = null;
    public string ReceivedBy { get; set; } = string.Empty;
    public string? DocRemarks { get; set; } = null;
    public string PreparedBy { get; set; } = string.Empty;
    public string? ApprovedBy { get; set; } = null;
    public string? NotedBy { get; set; } = null;
    public IEnumerable<GoodsIssueLineVM> DocumentLines { get; set; } = [];
    public WarehouseVM? Warehouse { get; set; } = null;
}
