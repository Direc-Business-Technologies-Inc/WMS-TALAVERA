using Web.BlazorServer.ViewModels.Transaction.Commons;

namespace Web.BlazorServer.ViewModels.Transaction.InventoryCounting;

public class InventoryCountingLineVM : ItemVM
{
    public decimal ActualQuantity { get; set; }
    public string? ISBN { get; set; }
}
