using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Transactions.Commons;

namespace Application.DataTransferObjects.Transactions.Delivery;

public class SalesOrderLineDTO:ItemDTO
{
    public WarehouseDTO Warehouse { get; set; }
    public decimal TargetQty { get; set; }
    public decimal OpenQty { get; set; }
}
