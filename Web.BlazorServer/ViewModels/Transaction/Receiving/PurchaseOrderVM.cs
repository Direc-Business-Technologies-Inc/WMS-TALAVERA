using Web.BlazorServer.ViewModels.Transaction.Commons;

namespace Web.BlazorServer.ViewModels.Transaction.Receiving;

public class PurchaseOrderVM : MarketingDocumentVM
{
    public string Remarks { get; set; } = string.Empty;
    public string SupplierContactPerson { get; set; } = string.Empty;
    public string PreparedBy { get; set; }
    public string? PONo { get; set; }
    public string? DRNo { get; set; }
    public string? Designation { get; set; }
    public string? ReceivedBy { get; set; }
    public string? ApprovedBy { get; set; }
    public string? NotedBy { get; set; }
    public string? SchoolYear { get; set; }
    public string? SINo { get; set; }
    public string? DeliveredBy { get; set; }
    public string? ReviewedBy { get; set; }
    public string? PurchaseType { get; set; }
    public string? ItemName { get; set; }
    public string? DocRemarks { get; set; }
    public int? Time { get; set; }
    public List<string> ItemGroupCodes { get; set; } = [];
    public List<PurchaseOrderLineVM> DocumentLines { get; set; } = [];

    public bool Validate() => !string.IsNullOrEmpty(ReceivedBy) && !DocumentLines.All(dl => dl.Quantity <= 0 && dl.OpenQuantity > 0);
}
