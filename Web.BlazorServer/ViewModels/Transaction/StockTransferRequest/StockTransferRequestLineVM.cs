namespace Web.BlazorServer.ViewModels.Transaction.StockTransferRequest;

public class StockTransferRequestLineVM
{
    public int ItemId { get; set; } 
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public string UoM { get; set; } = string.Empty;
    public string Warehouse { get; set; } = string.Empty;
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityAlloted { get; set; }

}
