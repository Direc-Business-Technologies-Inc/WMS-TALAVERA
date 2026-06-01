using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.Transaction.Commons;

namespace Web.BlazorServer.ViewModels.Transaction.SalesReturn;

public class SalesReturnRequestLineVM : ItemVM
{
    public int DocEntry { get; set; }
    public int DocNum { get; set; }
    public decimal TargetQuantity { get; set; }
    public decimal OpenQuantity { get; set; }
    public WarehouseVM Warehouse { get; set; } = new();
}
