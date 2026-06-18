using Web.BlazorServer.Components.Custom.Utilities;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.ViewModels.Transaction.InventoryAdjustment;

public class InventoryAdjustmentDataGridVM
{
    [QuickDataGridIgnore]
    public int Id { get; set; }
    [QuickDataGridTitle("Reference Number")]
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Subsidiary { get; set; } = string.Empty;
    [QuickDataGridTitle("Warehouse")]
    public string Location { get; set; } = string.Empty;
    public string Account { get; set;  } = string.Empty;
    [QuickDataGridTitle("Remarks")]
    public string Memo { get; set; } = string.Empty;
    [QuickDataGridTitle("Prepared By")]
    public string PreparedBy { get; set; } = string.Empty;
}
