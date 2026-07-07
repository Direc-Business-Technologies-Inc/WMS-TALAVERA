namespace Web.BlazorServer.ViewModels.Transaction.Packing.STR;

public class StockTransferRequestLinePackingVM
{
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public string UoM { get; set; } = string.Empty;
    public string Warehouse { get; set; } = string.Empty;
    public decimal QuantityPlanned { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityBackOrdered { get; set; }
}
