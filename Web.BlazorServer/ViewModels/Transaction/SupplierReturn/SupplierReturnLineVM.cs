using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.ViewModels.Transaction.SupplierReturn;

public class SupplierReturnLineVM
{
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public ItemUnitVM? UoM { get; set; }
    public LocationVM? Location { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityAlloted { get; set; }
    public decimal QuantityOnHandByUoM => QuantityOnHand / (UoM?.ConversionRate ?? 1);
    public int? LineNumber { get; set; }
}
