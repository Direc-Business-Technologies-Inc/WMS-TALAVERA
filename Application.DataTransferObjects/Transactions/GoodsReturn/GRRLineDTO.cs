using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Transactions.Commons;

namespace Application.DataTransferObjects.Transactions.GoodsReturn;

public class GRRLineDTO : ItemDTO
{
    public int DocEntry { get; set; }
    public int DocNum { get; set; }
    public string WhsCode { get; set; }
    public string WhsName { get; set; }
    public decimal TargetQty { get; set; }
    public decimal OpenQty { get; set; }
    public WarehouseDTO Warehouse { get; set; }
}
