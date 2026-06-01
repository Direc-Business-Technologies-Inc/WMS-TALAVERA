using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Transactions.Commons;

namespace Application.DataTransferObjects.Transactions.GoodsReturn;

public class GoodsReturnLineDTO : ItemDTO
{
    public int DocEntry { get; set; }
    public int DocNum { get; set; }
    public int BaseEntry { get; set; }
    public int BaseDocNum { get; set; }
    public int BaseLine { get; set; }
    public WarehouseDTO Warehouse { get; set; }
}
