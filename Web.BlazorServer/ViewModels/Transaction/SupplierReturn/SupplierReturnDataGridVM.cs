using Web.BlazorServer.Components.Custom.Utilities;

namespace Web.BlazorServer.ViewModels.Transaction.SupplierReturn;

public class SupplierReturnDataGridVM
{
    [QuickDataGridTitle("Document Number")]
    public string ReferenceNumber { get; set; } = string.Empty;
    [QuickDataGridTitle("Date")]
    [QuickDataGridStringFormat("{0:MMMM dd, yyyy}")]
    public DateTime Date { get; set; }
    [QuickDataGridTitle("Vendor")]
    public string VendorName { get; set; } = string.Empty;
    [QuickDataGridTitle("Created From")]
    public string CreatedFrom { get; set; } = string.Empty;
    [QuickDataGridTitle("Category")]
    public string CategoryName { get; set; } = string.Empty;
    [QuickDataGridTitle("Remarks")]
    public string Memo { get; set; } = string.Empty;
    [QuickDataGridTitle("Prepared By")]
    public string PreparedBy { get; set; } = string.Empty;
    [QuickDataGridTitle("Status")]
    public string StatusName { get; set; } = string.Empty;
}
