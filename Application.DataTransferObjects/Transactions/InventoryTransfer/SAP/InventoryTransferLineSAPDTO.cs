
namespace Integration.SAP.Entities.Transactional.InventoryTransfer;

public class InventoryTransferLineSAPDTO
{
    public string ItemCode { get; set; }
    public string ItemDescription { get; set; }
    public string UoMName { get; set; }
    public int LineNum { get; set; }
    public Decimal Quantity { get; set; }
}
