using Application.DataTransferObjects.Others.NS;

namespace Application.DataTransferObjects.Transactions.Packing.NS;

public class VendorReturnAuthorizationLineDTO : TransactionDTO
{
    public string LocationUsedBin { get; set; } = string.Empty;


    public int NetsuiteVendorInternalId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public int VendorBinAssignmentId { get; set; }

    public int NetsuiteMaterialPrefferedBinId { get; set; }
}
