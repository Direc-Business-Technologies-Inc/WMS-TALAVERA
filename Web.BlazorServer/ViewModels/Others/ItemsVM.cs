namespace Web.BlazorServer.ViewModels.Others;

public class ItemsVM
{
    public int Id { get; set; }
    public string ItemNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public string PurchaseUnit { get; set; } = string.Empty;
    public int PurchaseUnitId { get; set; }

    public string StockUnit { get; set; } = string.Empty;
    public int StockUnitId { get; set; }

    public string SaleUnit { get; set; } = string.Empty;
    public int SaleUnitId { get; set; }
}
