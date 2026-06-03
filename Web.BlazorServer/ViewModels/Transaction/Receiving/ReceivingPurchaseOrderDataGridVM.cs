using Domain.Entities.Enums.Transaction.Commons;

namespace Web.BlazorServer.ViewModels.Transaction.Receiving;

public class ReceivingPurchaseOrderDataGridVM
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public string Vendor { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
}
