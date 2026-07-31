using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.ViewModels.Transaction.InventoryAdjustment;

public class InventoryAdjustmentVM
{
    public int Id { get; set; }
    public SubsidiaryVM? Subsidiary { get; set; }
    public LocationVM? Location { get; set; }
    public DepartmentVM? Department { get; set; }
    public BusinessAccountVM? Account { get; set; }
    public InventoryAdjustmentReasonVM? Reason { get; set; } = null;
    public InventoryAdjustmentCategoryVM? Category { get; set; } = null;
    public string Memo { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string PreparedBy { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public List<InventoryAdjustmentLineVM> Lines { get; set; } = [];
    public int IssueLinesCount => Lines.Count(x => x.Type == InventoryAdjustmentLineVM.Types.Issue);
    public int ReceiptLinesCount => Lines.Count(x => x.Type == InventoryAdjustmentLineVM.Types.Receipt);
    public bool IsIndeterminate => Lines.Count == 0 || (IsIssue && IsReceipt);
    public bool IsIssue => IssueLinesCount > 0;
    public bool IsReceipt => ReceiptLinesCount > 0;
    public string TypeString
    {
        get
        {
            if (IsIndeterminate) return "Indeterminate";
            if (IsIssue) return "Issue";
            return "Receipt";
        }
    }
}
