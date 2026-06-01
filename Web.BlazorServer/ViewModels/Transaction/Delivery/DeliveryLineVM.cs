using Application.DataTransferObjects.Others;
using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.Transaction.Commons;

namespace Web.BlazorServer.ViewModels.Transaction.Delivery;

public class DeliveryLineVM : ItemVM
{
    public WarehouseVM? Warehouse { get; set; }
}
