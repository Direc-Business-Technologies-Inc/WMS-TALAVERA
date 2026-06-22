namespace Shared.Libraries.ViewModel.PurchaseOrder;

public class PurchaseOrderLineVM : TransactionVM
{
    public int NetsuiteSubsidiaryDefaultBOInternalId { get; set; }

    public int NetsuiteVendorInternalId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public int VendorBinAssignmentId { get; set; }

    public int NetsuiteMaterialPrefferedBinId { get; set; }
}
