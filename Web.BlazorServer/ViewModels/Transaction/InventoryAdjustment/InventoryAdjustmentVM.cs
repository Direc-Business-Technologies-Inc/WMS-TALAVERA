using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.ViewModels.Transaction.InventoryAdjustment;

public class InventoryAdjustmentVM
{
    public int Id { get; set; }
    public SubsidiaryVM? Subsidiary { get; set; }
    public LocationVM? Location { get; set; }
    public BusinessAccountVM? Account { get; set; }
    public InventoryAdjustmentReasonVM? Reason { get; set; } = null;
    public string Memo { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string PreparedBy { get; set; } = string.Empty;
    public DateTime Date  { get; set; }
    public List<InventoryAdjustmentLineVM> Lines { get; set; } = [];
}
