using Web.BlazorServer.ViewModels.Transaction.Commons;

namespace Web.BlazorServer.ViewModels.Transaction.InventoryTransfer;

public class InventoryTransferCVULineVM : ItemVM
{
    public string ItemDescription { get; set; }
    public Decimal Quantity { get; set; }
}
