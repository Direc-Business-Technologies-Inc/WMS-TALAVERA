using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.Transaction.Commons;

namespace Web.BlazorServer.ViewModels.Transaction.Delivery;

public class SalesOrderLineVM : ItemVM
{
    public WarehouseVM? Warehouse { get; set; }
    public decimal TargetQty { get; set; }
    public decimal OpenQty { get; set; }

}
