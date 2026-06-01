
using System.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Integration.SAP.Entities.Transactional.InventoryTransfer;

public class InventoryTransferRequestLineSAPDTO
{
    public string ItemCode { get; set; }
    public string ItemName { get; set; }
    public int LineNum { get; set; }
    public string UoMName { get; set; }
    public Decimal Quantity { get; set; }
    public Decimal OpenQuantity { get; set; }
    public Decimal PendingQuantity { get; set; }
    public Decimal AllotedQuantity { get; set; }
}