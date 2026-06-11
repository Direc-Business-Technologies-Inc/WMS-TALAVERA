using Domain.Entities.Enums.Transaction.Commons;

namespace Web.BlazorServer.ViewModels.Transaction.Receiving;

public class TransferOrderDataGridVM
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public string SourceSubsidiary { get; set; } = string.Empty;
    public string DesctinationSubsidiary { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string TransferLocation { get; set; } = string.Empty;
}
