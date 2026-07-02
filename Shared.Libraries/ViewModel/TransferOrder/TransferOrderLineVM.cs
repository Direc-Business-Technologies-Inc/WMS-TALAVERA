namespace Shared.Libraries.ViewModel.TransferOrder;

public class TransferOrderLineVM : TransactionVM
{
    public int NetsuiteFromLocationInternalId { get; set; }
    public int NetsuiteToLocationInternalId { get; set; }

    public int NetsuiteFromSubsidiaryInternalId { get; set; }
    public int NetsuiteSubsidiaryDefaultBOInternalId { get; set; }
    public int NetsuiteToSubsidiaryInternalId { get; set; }

    public int NetsuiteMaterialPrefferedBinId { get; set; }
    public int NetsuiteMaterialVendorAssignedBin { get; set; }
}
