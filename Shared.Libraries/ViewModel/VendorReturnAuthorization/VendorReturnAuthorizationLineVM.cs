namespace Shared.Libraries.ViewModel.VendorReturnAuthorization;
public class VendorReturnAuthorizationLineVM : TransactionVM
{
    public int NetsuiteVendorInternalId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public int VendorBinAssignmentId { get; set; }

    public int NetsuiteMaterialPrefferedBinId { get; set; }
}
