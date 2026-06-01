using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Transactions.Commons;

namespace Application.DataTransferObjects.Transactions.SalesReturn;

public class SalesReturnRequestLineDTO : ItemDTO
{
    public int DocEntry { get; set; }
    public int DocNum { get; set; }
    public decimal TargetQuantity { get; set; }
    public decimal OpenQuantity { get; set; }
    public WarehouseDTO? Warehouse { get; set; }
}
