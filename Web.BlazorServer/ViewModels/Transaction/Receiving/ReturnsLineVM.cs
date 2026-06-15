namespace Web.BlazorServer.ViewModels.Transaction.Receiving;

public class ReturnsLineVM
{
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public string UoM { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public decimal QuantityPlanned { get; set; }
}