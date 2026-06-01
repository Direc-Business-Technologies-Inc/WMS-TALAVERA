namespace Web.BlazorServer.ViewModels.Transaction.Commons;

public class ItemVM
{
    public int LineNum { get; set; }
    public string ItemCode { get; set; }
    public string ItemName { get; set; }
    public decimal Quantity { get; set; }
    public string? ISBN { get; set; }
    public string UoMCode { get; set; }
    public decimal UoMValue { get; set; }
    public string UoMName { get; set; } = string.Empty;
}
