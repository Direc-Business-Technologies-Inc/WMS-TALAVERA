namespace Web.BlazorServer.ViewModels.Others;

public class ItemsVM
{
    public int Id { get; set; }
    public string ItemNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UnitTypeName { get; set; } = string.Empty;
    public int UnitTypeId { get; set; }
    public ItemUnitVM PurchaseUnit { get; set; } = new();
    public ItemUnitVM StockUnit { get; set; } = new(); 
    public ItemUnitVM SaleUnit { get; set; } = new();
    public bool UsesBins { get; set; }
}
