using Domain.Entities.Enums.Transaction.Commons;
using Shared.Kernel;
using Web.BlazorServer.ViewModels.Transaction.Commons;

namespace Web.BlazorServer.ViewModels.Transaction.Receiving;

public class PurchaseOrderVM
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime DeliveryDate { get; set; }
    public string VendorCode { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public string Memo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<PurchaseOrderLineVM> DocumentLines { get; set; } = [];
    public string StatusDescription => EnumHelper.GetEnumDescription(EnumHelper.ParseStringToEnum<DocumentStatus>(Status));
}
