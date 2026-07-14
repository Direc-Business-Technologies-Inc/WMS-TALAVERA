using Web.BlazorServer.Components.Custom.Utilities;

namespace Web.BlazorServer.ViewModels.Transaction.InventoryTransferRequest;

public class InventoryTransferRequestDataGridVM
{
    [QuickDataGridIgnore]
    public int Id { get; set; }
    [QuickDataGridTitle("Reference Number")]
    public string ReferenceNumber { get; set; } = string.Empty;
    [QuickDataGridStringFormat("{0:MMMM dd, yyyy}")]
    public DateTime Date { get; set; }
    [QuickDataGridTitle("Subsidiary")]
    public string SubsidiaryName { get; set; } = string.Empty;
    [QuickDataGridTitle("From Warehouse")]
    public string SourceLocation { get; set; } = string.Empty;
    [QuickDataGridTitle("To Warehouse")]
    public string DestinationLocation { get; set; } = string.Empty;
    [QuickDataGridTitle("Prepared By")]
    public string PreparedBy { get; set; } = string.Empty;
    [QuickDataGridTitle("Status")]
    public string StatusName { get; set; } = string.Empty;
    [QuickDataGridTitle("Remarks")]
    public string Memo { get; set; } = string.Empty;
}