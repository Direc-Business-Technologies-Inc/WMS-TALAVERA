using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Transactions.Commons;

namespace Application.DataTransferObjects.Transactions.GoodsReturn;

public class GoodsReturnDTO : MarketingDocumentDTO
{
    public int GRRDocEntry { get; set; }
    public int GRRDocNum { get; set; }
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
    public WarehouseDTO? Warehouse { get; set; } = null;
    public List<GoodsReturnLineDTO> DocumentLines { get; set; } = [];
}
