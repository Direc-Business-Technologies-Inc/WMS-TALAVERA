using Domain.Entities.Enums.Transaction.Commons;
using Web.BlazorServer.Components.Custom.Utilities;

namespace Web.BlazorServer.ViewModels.Transaction.Receiving;

public class PurchaseOrderDataGridVM
{
    [QuickDataGridIgnore]
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public string Vendor { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
}
