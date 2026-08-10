using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.ViewModels.Transaction.StockTransferRequest;

public class StockTransferRequestLineVM
{
    public int? LineNumber { get; set; }
    public int ItemId { get; set; } 
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public ItemUnitVM? UoM { get; set; }
    public string Warehouse { get; set; } = string.Empty;
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityAvailable { get; set; }
    public decimal QuantityOnHandByUoM => QuantityOnHand / (UoM?.ConversionRate ?? 1);
    public decimal QuantityAlloted { get; set; }

}
