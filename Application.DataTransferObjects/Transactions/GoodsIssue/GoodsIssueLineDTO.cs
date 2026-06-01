using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Transactions.Commons;

namespace Application.DataTransferObjects.Transactions.GoodsIssue;

public class GoodsIssueLineDTO : ItemDTO
{
    public WarehouseDTO Warehouse { get; set; }
}
