using Web.BlazorServer.ViewModels.Transaction.Commons;

namespace Web.BlazorServer.ViewModels.Transaction.InventoryTransfer
{
    public class InventoryTransferRequestLineVM : ItemVM
    {
        public Decimal OpenQuantity { get; set; }
        public Decimal PendingQuantity { get; set; }
        public Decimal AllotedQuantity { get; set; }
        public Decimal OnHandQuantity { get; set; }
    }
}
