namespace Web.BlazorServer.ViewModels.Transaction.StockTransferRequest;

public class StockTransferRequestLineVM
{
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public string UoM { get; set; } = string.Empty;
    public string Warehouse { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public int QuantityAlloted { get; set; }

}
