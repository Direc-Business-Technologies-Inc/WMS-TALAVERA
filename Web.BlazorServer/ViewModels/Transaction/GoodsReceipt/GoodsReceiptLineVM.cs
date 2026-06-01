using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.Transaction.Commons;

namespace Web.BlazorServer.ViewModels.Transaction.GoodsReceipt;

public class GoodsReceiptLineVM : ItemVM
{
    public decimal OnHandQty { get; set; } = 0;
    public WarehouseVM Warehouse { get; set; } = new();

}