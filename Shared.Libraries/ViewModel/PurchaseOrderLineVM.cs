namespace Shared.Libraries.ViewModel;

public class PurchaseOrderLineVM
{
    public int NetsuiteOrderInternalId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;

    public int NetsuiteSubsidiaryInternalId { get; set; }
    public int NetsuiteSubsidiaryDefaultBOInternalId { get; set; }

    public int NetsuiteLocationInternalId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string LocationUsedBin { get; set; }
    public bool IsLocationUsedBin => LocationUsedBin == "T";

    public int LineSequenceNumber { get; set; }
    public string TransactionLineType { get; set; } = string.Empty;

    public int NetsuiteVendorInternalId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public int VendorBinAssignmentId { get; set; }

    public int NetsuiteMaterialInternalId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public int NetsuiteMaterialPrefferedBinId { get; set; }
    public decimal MaterialWeight { get; set; }
    public decimal LineQuantity { get; set; } // DB Quantity
    public decimal LineQuantityReceived { get; set; }
    public int NetsuiteUoMInternalId { get; set; }
    public string UoMName { get; set; } = string.Empty;
    public int UoMRate { get; set; }

    public decimal TotalWeight => LineQuantity * MaterialWeight; // Record Weight

    public decimal NSLineQuantity => LineQuantity / UoMRate;
    public decimal NSLineQuantityReceived => LineQuantityReceived / UoMRate;

    // For Scanning
    public int ScanCount { get; set; }

    // Keep existing total scanned quantity for backward compat
    public int ScannedQuantity { get; set; }
    public decimal ScannedWeight { get; set; }
    public decimal TotalQuantity => ScannedQuantity + NSLineQuantityReceived;

    // New: classification flag - true = Bad, false = Good (default)
    public bool IsBad { get; set; } = false;

    public bool AlreadyFulfilled => ScannedQuantity == LineQuantity;
    public bool OverScanned => ScannedQuantity > LineQuantity;
}
