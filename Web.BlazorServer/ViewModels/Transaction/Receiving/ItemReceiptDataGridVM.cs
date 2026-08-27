using Web.BlazorServer.Components.Custom.Utilities;

namespace Web.BlazorServer.ViewModels.Transaction.Receiving;

public class ItemReceiptDataGridVM
{
    [QuickDataGridIgnore]
    public int Id { get; set; }
    [QuickDataGridTitle("Reference Number")]
    public string ReferenceNumber { get; set; } = string.Empty;
    [QuickDataGridTitle("Document Type")]
    public string DocumentType
    {
        get
        {
            var type = CreatedFrom.Split('-').FirstOrDefault();

            return type switch
            {
                "STR" => "Stock Transfer Request",
                "PO" => "Purchase Order",
                _ => string.Empty
            };
        }
    }
    [QuickDataGridTitle("Vendor")]
    public string Vendor { get; set; } = string.Empty;
    [QuickDataGridStringFormat("{0:MMMM dd, yyyy}")]
    public DateTime Date { get; set; }
    [QuickDataGridTitle("Transfer Category")]
    public string TransferCategory { get; set; } = string.Empty;
    [QuickDataGridTitle("Created From")]
    public string CreatedFrom { get; set; } = string.Empty;
    [QuickDataGridTitle("Source Warehouse")]
    public string FromLocation { get; set; } = string.Empty;
    [QuickDataGridTitle("Destination Warehouse")]
    public string ToLocation { get; set; } = string.Empty;
}
