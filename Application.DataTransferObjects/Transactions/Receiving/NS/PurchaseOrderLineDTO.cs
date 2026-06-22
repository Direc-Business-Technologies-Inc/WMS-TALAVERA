using Application.DataTransferObjects.Others.NS;

namespace Application.DataTransferObjects.Transactions.Receiving.NS;

public class PurchaseOrderLineDTO : TransactionDTO
{
    public int NetsuiteSubsidiaryDefaultBOInternalId { get; set; }
    public int NetsuitePrefferedBadBinId { get; set; }

    public string LocationUsedBin { get; set; } = string.Empty;

    public int NetsuiteVendorInternalId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public int VendorBinAssignmentId { get; set; }

    public int NetsuiteMaterialPrefferedBinId { get; set; } 

}
