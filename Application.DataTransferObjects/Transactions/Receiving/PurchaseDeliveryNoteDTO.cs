using Application.DataTransferObjects.Transactions.Commons;

namespace Application.DataTransferObjects.Transactions.Receiving;

public class PurchaseDeliveryNoteDTO : MarketingDocumentDTO
{
    public string SupplierContactPerson { get; set; }
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
    public List<PurchaseDeliveryNoteLineDTO> DocumentLines { get; set; } = [];
}
