using Web.BlazorServer.ViewModels.Transaction.Commons;

namespace Web.BlazorServer.ViewModels.Transaction.SalesReturn;

public class SalesReturnRequestVM : MarketingDocumentVM
{
    public string? ContactPerson { get; set; }
    public string? NumAtCard { get; set; }
    public string? SchoolYear { get; set; }
    public string? ReturnType { get; set; }
    public string? PURNo { get; set; }
    public string? DRNo { get; set; }
    public string? SONo { get; set; }
    public string? SINo { get; set; }
    public string? Designation { get; set; }
    public string? DocRemarks { get; set; }
    public string? ReturnedBy { get; set; }
    public string? PickBy { get; set; }
    public string PreparedBy { get; set; } = string.Empty;
    public string? CheckedBy { get; set; }
    public string? NotedBy { get; set; }
    public string? ApprovedBy { get; set; }

    public List<SalesReturnRequestLineVM> DocumentLines { get; set; } = [];
}
