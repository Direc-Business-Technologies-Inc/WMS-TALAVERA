using Application.DataTransferObjects.Transactions.Commons;

namespace Application.DataTransferObjects.Transactions.InventoryTransfer;

public class InventoryTransferLineDTO: ItemDTO
{
    public string ItemCode { get; set; }
    public string ItemDescription { get; set; }
    public string UoM { get; set; }
    public Decimal Quantity{ get; set; }
}
