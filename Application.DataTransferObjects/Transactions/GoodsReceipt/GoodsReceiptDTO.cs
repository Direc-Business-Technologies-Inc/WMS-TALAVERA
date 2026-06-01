using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Transactions.Commons;

namespace Application.DataTransferObjects.Transactions.GoodsReceipt;

public class GoodsReceiptDTO : MarketingDocumentDTO
{
    public string PreparedBy { get; set; }
    public TransactionTypeDTO TransactionType { get; set; }
    public string? WarNo { get; set; } = null;
    public string? PurNo { get; set; } = null;
    public string? DocRemarks { get; set; } = null;
    public string? Designation { get; set; } = null;
    public string? ReceivedBy { get; set; } = null;
    public string? ApprovedBy { get; set; } = null;
    public string? NotedBy { get; set; } = null;
    public IEnumerable<GoodsReceiptLineDTO> DocumentLines { get; set; } = [];
}
