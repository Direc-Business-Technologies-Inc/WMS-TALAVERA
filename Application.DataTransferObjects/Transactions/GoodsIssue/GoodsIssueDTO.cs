using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Transactions.Commons;
using Application.DataTransferObjects.Transactions.GoodsIssue;

namespace Application.DataTransferObjects.Transactions.Goodsissue;

public class GoodsIssueDTO : MarketingDocumentDTO
{
    public string? SchoolYear { get; set; }
    public TransactionTypeDTO TransactionType { get; set; }
    public string? SrfNo { get; set; }
    public string? Designation { get; set; }
    public string? ReceivedBy { get; set; }
    public string? DocRemarks { get; set; }
    public string PreparedBy { get; set; }
    public string? ApprovedBy { get; set; }
    public string? NotedBy { get; set; }
    public IEnumerable<GoodsIssueLineDTO> DocumentLines { get; set; } = [];
}
