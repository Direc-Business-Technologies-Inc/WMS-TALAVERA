namespace Web.BlazorServer.ViewModels.Others;

public class InventoryDetailVM
{
    public int? Id { get; set; } 
    public LocationBinVM? Bin { get; set; } = null;
    public InventoryStatusVM? Status { get; set; } 
    public decimal QuantityAlloted { get; set; }
}
