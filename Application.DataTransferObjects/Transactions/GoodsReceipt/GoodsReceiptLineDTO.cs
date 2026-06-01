using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Transactions.Commons;

namespace Application.DataTransferObjects.Transactions.GoodsReceipt;

public class GoodsReceiptLineDTO : ItemDTO
{
    public WarehouseDTO Warehouse { get; set; }
}
