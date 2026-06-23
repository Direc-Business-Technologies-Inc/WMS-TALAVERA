
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.ViewModels.Transaction.SupplierReturn;

public class SupplierReturnVM
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public VendorVM? Vendor { get; set; } = null;
    public LocationVM? Location { get; set; } = null;
    public ReturnStatusVM? Status { get; set; } = null;
    public ReturnCategoryVM? ReturnCategory { get; set; } = null;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Memo { get; set; } = string.Empty;
    public IEnumerable<SupplierReturnLineVM> Lines { get; set; } = [];
}
