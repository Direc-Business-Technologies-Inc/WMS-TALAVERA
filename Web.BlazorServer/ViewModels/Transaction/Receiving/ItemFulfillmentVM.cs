namespace Web.BlazorServer.ViewModels.Transaction.Receiving;

public class ItemFulfillmentVM
{
    [Components.Custom.Utilities.QuickDataGridIgnore]
    public int Id { get; set; }
    [Components.Custom.Utilities.QuickDataGridTitle("Reference Number")]

    public string ReferenceNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    [Components.Custom.Utilities.QuickDataGridTitle("Prepared By")]
    public string PreparedBy { get; set; } = string.Empty;
}

public class ItemFulfillmentLineVM
{
    public int ItemFullfillmentId { get; set; }
    public int LineNumber { get; set; }
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public decimal QuantityAlloted { get; set; }
    public decimal QuantityOpen { get; set; }
}