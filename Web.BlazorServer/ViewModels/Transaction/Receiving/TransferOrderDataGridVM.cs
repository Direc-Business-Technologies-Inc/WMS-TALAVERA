using Domain.Entities.Enums.Transaction.Commons;

namespace Web.BlazorServer.ViewModels.Transaction.Receiving;

public class TransferOrderDataGridVM
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public string FromSubsidiary { get; set; } = string.Empty;
    public string ToSubsidiary { get; set; } = string.Empty;
    public string SourceWarehouse { get; set; } = string.Empty;
    public string TransferWarehouse { get; set; } = string.Empty;
}
