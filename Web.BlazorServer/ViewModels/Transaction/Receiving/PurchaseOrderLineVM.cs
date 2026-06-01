using Application.DataTransferObjects.Others;
using Domain.Entities.Enums.Transaction.Receiving;
using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.Transaction.Commons;

namespace Web.BlazorServer.ViewModels.Transaction.Receiving;

public class PurchaseOrderLineVM : ItemVM
{
    public int DocEntry { get; set; }
    public int DocNum { get; set; }
    public bool Free { get; set; }
    public decimal Price { get; set; } = 0;
    public decimal OpenQuantity { get; set; } = 0;
    public decimal TargetQuantity { get; set; } = 0;
    public WarehouseVM Warehouse { get; set; }
    public string VatGroup { get; set; }
    public InputType InputType { get; set; } = InputType.Manual;
}
