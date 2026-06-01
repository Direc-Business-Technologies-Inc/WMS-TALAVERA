using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Transactions.Commons;

namespace Application.DataTransferObjects.Transactions.SalesReturn;

public class SalesReturnLineDTO : ItemDTO
{
    public int DocEntry { get; set; }
    public int DocNum { get; set; }
    public int BaseEntry { get; set; }
    public int BaseDocNum { get; set; }
    public int BaseLine { get; set; }
    public decimal TargetQuantity { get; set; }
    public decimal OpenQuantity { get; set; }
    public WarehouseDTO? Warehouse { get; set; }
}
