using Application.DataTransferObjects.Transactions.Commons;

namespace Application.DataTransferObjects.Transactions.Delivery;

public class SalesOrderDTO : MarketingDocumentDTO
{
    public string? ContactPerson { get; set; } = null;
    public string? DRNo { get; set; } = null;
    public string? SchoolYear { get; set; } = null;
    public string? PONo { get; set; } = null;
    public string? Area { get; set; } = null;
    public string? Designation { get; set; } = null;
    public string? OrderedBy { get; set; } = null;
    public string? DocRemarks { get; set; } = null;
    public string? PreparedBy { get; set; } = null;
    public string? ReviewedBy { get; set; } = null;
    public string? ApprovedBy { get; set; } = null;
    public string? NotedBy { get; set; }
    public IEnumerable<SalesOrderLineDTO> DocumentLines { get; set; } = [];
}
