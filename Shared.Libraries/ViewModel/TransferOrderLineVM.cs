namespace Shared.Libraries.ViewModel;

public class TransferOrderLineVM
{
    public int NetsuiteOrderInternalId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;

    public int NetsuiteFromLocationInternalId { get; set; }
    public int NetsuiteToLocationInternalId { get; set; }

    public int NetsuiteFromSubsidiaryInternalId { get; set; }
    public int NetsuiteSubsidiaryDefaultBOInternalId { get; set; }
    public int NetsuiteToSubsidiaryInternalId { get; set; }

    public string LocationName { get; set; } = string.Empty;
    public string LocationUsedBin { get; set; }
    public bool IsLocationUsedBin => LocationUsedBin == "T";

    public int LineSequenceNumber { get; set; }
    public string TransactionLineType { get; set; } = string.Empty;

    public int NetsuiteMaterialInternalId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public int NetsuiteMaterialPrefferedBinId { get; set; }
    public decimal MaterialWeight { get; set; }

    public decimal LineQuantity { get; set; }
    public decimal LineQuantityReceived { get; set; }
    public int NetsuiteUoMInternalId { get; set; }
    public string UoMName { get; set; } = string.Empty;
    public decimal UoMRate { get; set; }

    public DateTime NetsuiteOrderCreatedDate { get; set; }
    public DateTime NetsuiteOrderDocumentDate { get; set; }
    public DateTime NetsuiteOrderUpdatedDate { get; set; }


    public decimal TotalWeight => LineQuantity * MaterialWeight; // Record Weight

    public decimal NSLineQuantity => LineQuantity / UoMRate;
    public decimal NSLineQuantityReceived => LineQuantityReceived / UoMRate;

    // For Scanning
    public int ScanCount { get; set; }

    // Keep existing total scanned quantity for backward compat
    public decimal ScannedQuantity { get; set; }
    public decimal ScannedWeight { get; set; }
    public decimal TotalQuantity => ScannedQuantity + NSLineQuantityReceived;

    // New: classification flag - true = Bad, false = Good (default)
    public bool IsBad { get; set; } = false;

    public bool AlreadyFulfilled => ScannedQuantity == LineQuantity;
    public bool OverScanned => ScannedQuantity > LineQuantity;
}
