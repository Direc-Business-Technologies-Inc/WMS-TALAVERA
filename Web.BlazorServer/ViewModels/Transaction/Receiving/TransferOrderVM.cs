using Domain.Entities.Enums.Transaction.Commons;
using Shared.Kernel;

namespace Web.BlazorServer.ViewModels.Transaction.Receiving;
public class TransferOrderVM
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime DeliveryDate { get; set; }
    public string RequestorName { get; set; } = string.Empty;
    public string SourceLocation { get; set; } = string.Empty;
    public string DestinationLocation { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<PurchaseOrderLineVM> DocumentLines { get; set; } = [];
    public string StatusDescription => EnumHelper.GetEnumDescription(EnumHelper.ParseStringToEnum<DocumentStatus>(Status));
}
