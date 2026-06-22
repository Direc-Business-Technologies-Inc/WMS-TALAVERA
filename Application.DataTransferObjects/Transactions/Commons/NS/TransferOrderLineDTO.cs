using Application.DataTransferObjects.Others.NS;

namespace Application.DataTransferObjects.Transactions.Commons.NS;

public class TransferOrderLineDTO : TransactionDTO
{
    public int TransferCategory { get; set; }

    public int NetsuiteFromLocationInternalId { get; set; }
    public int NetsuiteToLocationInternalId { get; set; }

    public int NetsuiteFromSubsidiaryInternalId { get; set; }
    public int NetsuiteSubsidiaryDefaultBOInternalId { get; set; }
    public int NetsuitePrefferedBadBinId { get; set; }
    public int NetsuiteToSubsidiaryInternalId { get; set; }

    public string LocationUsedBin { get; set; } = string.Empty;

    public int NetsuiteMaterialPrefferedBinId { get; set; }
    public int NetsuiteMaterialVendorAssignedBin { get; set; }
}
