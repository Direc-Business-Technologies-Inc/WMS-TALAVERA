
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.ViewModels.Transaction.InventoryTransferRequest;

public class InventoryTransferRequestVM
{

    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public SubsidiaryVM? Subsidiary { get; set; }
    public LocationVM? SourceLocation { get; set; }
    public LocationVM? DestinationLocation { get; set; }
    public string PreparedBy { get; set; } = string.Empty;
    public string Memo { get; set; } = string.Empty;

    public List<InventoryTransferRequestLineVM> Lines = [];
}
