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
    public string VendorName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    [QuickDataGridIgnore]
    public string StatusId { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
}
