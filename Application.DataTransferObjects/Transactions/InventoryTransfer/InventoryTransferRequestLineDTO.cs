using Application.DataTransferObjects.Transactions.Commons;

namespace Application.DataTransferObjects.Transactions.InventoryTransfer;

public class InventoryTransferRequestLineDTO : ItemDTO
{
    public Decimal OpenQuantity { get; set; }
    public Decimal PendingQuantity { get; set; }
    public Decimal AllotedQuantity { get; set; }
}
