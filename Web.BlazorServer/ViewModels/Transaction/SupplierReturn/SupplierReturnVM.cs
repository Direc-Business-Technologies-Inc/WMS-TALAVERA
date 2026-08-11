
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.ViewModels.Transaction.SupplierReturn;

public class SupplierReturnVM
{
    public int? Id { get; set; }
    public DateTime Date { get; set; }
    public VendorVM? Vendor { get; set; } = null;
    public LocationVM? Location { get; set; } = null;
    public SubsidiaryVM? Subsidiary { get; set; } = null;
    public ReturnStatusVM? Status { get; set; } = null;
    public ReturnCategoryVM? ReturnCategory { get; set; } = null;
    public PurchaseSubcategoryVM? PurchaseSubcategory { get; set; } = null;
    public PurchaseCategoryVM? PurchaseCategory { get; set; } = null;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Memo { get; set; } = string.Empty;
    public string PreparedBy { get; set; } = string.Empty;
    public string CreatedFrom { get; set; } = string.Empty;
    public bool IsSubmittedForApprovals { get; set; }
    public List<SupplierReturnLineVM> Lines { get; set; } = [];

    public int? SourcePO { get; set; }
}
