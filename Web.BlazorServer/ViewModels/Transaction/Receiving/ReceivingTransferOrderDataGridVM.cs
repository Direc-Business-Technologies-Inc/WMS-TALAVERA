using Domain.Entities.Enums.Transaction.Commons;

namespace Web.BlazorServer.ViewModels.Transaction.Receiving;

public class ReceivingTransferOrderDataGridVM
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public string RequestorName { get; set; } = string.Empty;
    public string SourceLocation { get; set; } = string.Empty;
    public string TransferLocation { get; set; } = string.Empty;
}
