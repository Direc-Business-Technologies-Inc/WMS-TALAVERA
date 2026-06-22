using Application.DataTransferObjects.Others.NS;

namespace Application.DataTransferObjects.Transactions.Commons.NS;

public class PostTransferOrderDTO : TransactionDTO
{
    public int TransferCategory { get; set; }

    public int NetsuiteFromLocationInternalId { get; set; }
    public int NetsuiteToLocationInternalId { get; set; }

    public int NetsuiteFromSubsidiaryInternalId { get; set; }
    public int NetsuiteSubsidiaryDefaultBOInternalId { get; set; }
    public int NetsuitePrefferedBadBinId { get; set; }
    public int NetsuiteToSubsidiaryInternalId { get; set; }

    public string LocationUsedBin { get; set;  } = string.Empty;
    public bool IsLocationUsedBin { get; set; }

    public int NetsuiteMaterialPrefferedBinId { get; set; }
    public int NetsuiteMaterialVendorAssignedBin { get; set; }

    public decimal TotalWeight { get; set;  }

    public decimal NSLineQuantity { get; set; }
    public decimal NSLineQuantityReceived { get; set; }
    public decimal NSLineQuantityPacked { get; set; }
    public decimal NSLineQuantityShipped { get; set; }

    public int ScanCount { get; set; }

    public decimal ScannedQuantity { get; set; }
    public decimal ScannedWeight { get; set; }
    public decimal TotalQuantity { get; set; }

    public bool IsBad { get; set; }

    public bool AlreadyFulfilled { get; set; }
    public bool OverScanned { get; set; }
}
