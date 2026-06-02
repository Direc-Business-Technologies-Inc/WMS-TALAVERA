using Domain.Entities.Enums.Transaction.Commons;

namespace Application.DataTransferObjects.Transactions.Receiving;

public class PurchaseOrderDataGridDTO
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime DeliveryDate { get; set; }
    public DocumentStatus Status { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string Memo { get; set; } = string.Empty;
}
