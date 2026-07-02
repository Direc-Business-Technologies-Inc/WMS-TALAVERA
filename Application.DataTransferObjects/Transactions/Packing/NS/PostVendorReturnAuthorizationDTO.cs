using Application.DataTransferObjects.Others.NS;

namespace Application.DataTransferObjects.Transactions.Packing.NS;

public class PostVendorReturnAuthorizationDTO : TransactionDTO
{
    public string LocationUsedBin { get; set; } = string.Empty;
    public bool IsLocationUsedBin { get; set; }

    public int NetsuiteVendorInternalId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public int VendorBinAssignmentId { get; set; }

    public int NetsuiteMaterialPrefferedBinId { get; set; }
    public int NetsuiteMaterialVendorAssignedBin { get; set; }

    public decimal TotalWeight { get; set; }

    public decimal NSLineQuantity { get; set; }
    public decimal NSLineQuantityReceived { get; set; }
    public decimal NSLineQuantityPacked { get; set; }
    public decimal NSLineQuantityShipped { get; set; }

    public int ScanCount { get; set; }

    public decimal ScannedQuantity { get; set; }
    public decimal ScannedWeight { get; set; }

    public bool IsBad { get; set; }
    public bool AlreadyFulfilled { get; set; }
}
