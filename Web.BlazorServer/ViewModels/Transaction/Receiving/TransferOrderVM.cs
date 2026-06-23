using Domain.Entities.Enums.Transaction.Commons;
using Shared.Kernel;

namespace Web.BlazorServer.ViewModels.Transaction.Receiving;
public class TransferOrderVM
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string FromSubsidiary { get; set; } = string.Empty;
    public string ToSubsidiary { get; set; } = string.Empty;
    public string SourceWarehouse { get; set; } = string.Empty;
    public string DestinationWarehouse { get; set; } = string.Empty;
    public string PreparedBy { get; set; } = string.Empty;
    public string ReceivedBy { get; set; } = string.Empty;
    public List<TransferOrderLineVM> Lines { get; set; } = [];
}
