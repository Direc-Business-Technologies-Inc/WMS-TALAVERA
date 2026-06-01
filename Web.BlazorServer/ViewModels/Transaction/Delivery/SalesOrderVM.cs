using Web.BlazorServer.ViewModels.Transaction.Commons;

namespace Web.BlazorServer.ViewModels.Transaction.Delivery;

public class SalesOrderVM : MarketingDocumentVM
{
    public string? ContactPerson { get; set; }
    public string? DRNo { get; set; }
    public string? SchoolYear { get; set; }
    public string? PONo { get; set; }
    public string? Area { get; set; }
    public string? Designation { get; set; }
    public string? OrderedBy { get; set; }
    public string? DocRemarks { get; set; }
    public string? PreparedBy { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ApprovedBy { get; set; }
    public string? NotedBy { get; set; }
    public IEnumerable<SalesOrderLineVM> DocumentLines { get; set; } = [];
}
