using Application.DataTransferObjects.Others.NS;

namespace Application.DataTransferObjects.Transactions.Receiving.NS;

public class PostPurchaseOrderDTO : TransactionDTO
{
    public int NetsuiteSubsidiaryDefaultBOInternalId { get; set; }
    public int NetsuitePrefferedBadBinId { get; set; }

    public string LocationUsedBin { get; set; } = string.Empty;
    public bool IsLocationUsedBin { get; set; }

    public int NetsuiteVendorInternalId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public int VendorBinAssignmentId { get; set; }

    public int NetsuiteMaterialPrefferedBinId { get; set; }

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
