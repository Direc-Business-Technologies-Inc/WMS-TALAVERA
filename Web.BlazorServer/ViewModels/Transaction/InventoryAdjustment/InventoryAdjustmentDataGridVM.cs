using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.ViewModels.Transaction.InventoryAdjustment;

public class InventoryAdjustmentDataGridVM
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Subsidiary { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Account { get; set;  } = string.Empty;
    public string Memo { get; set; } = string.Empty;
    public string PreparedBy { get; set; } = string.Empty;
}
