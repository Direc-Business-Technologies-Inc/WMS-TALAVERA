using Application.DataTransferObjects.Transactions.GoodsReturn;
using Web.BlazorServer.ViewModels.Transaction.Commons;

namespace Web.BlazorServer.ViewModels.Transaction.GoodsReturn;

public class GoodsReturnRequestVM : MarketingDocumentVM
{
    public int GRPODocEntry { get; set; }
    public int GRPODocNum { get; set; }
    public int PODocEntry { get; set; }
    public int PODocNum { get; set; }
    public string PreparedBy { get; set; }
    public string ReturnType { get; set; }
    public string? SchoolYear { get; set; }
    public string? DRNo { get; set; }
    public string? SINo { get; set; }
    public string? DeliveredBy { get; set; }
    public string? ReceivedBy { get; set; }
    public string? DocRemarks { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ApprovedBy { get; set; }
    public string? CheckedBy { get; set; }

    public List<GRRLineVM> DocumentLines { get; set; } = [];
}
