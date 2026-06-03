namespace Shared.Libraries.ViewModel;

public class PurchaseOrderLineVM
{
    public int NetsuiteOrderInternalId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;

    public string NetsuiteLocationInternalId { get; set; } = string.Empty;
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
    public int LineQuantity { get; set; }
    public int LineQuantityReceived { get; set; }
    public int NetsuiteUoMInternalId { get; set; }
    public string UoMName { get; set; } = string.Empty;
    public int UoMRate { get; set; }


    // For Scanning
    public int ScanCount { get; set; }

    // Keep existing total scanned quantity for backward compat
    public int ScannedQuantity { get; set; }
    public int TotalQuantity => ScannedQuantity + LineQuantityReceived;

    // New: classification flag - true = Bad, false = Good (default)
    public bool IsBad { get; set; } = false;

    public bool AlreadyFulfilled => ScannedQuantity == LineQuantity;
    public bool OverScanned => ScannedQuantity > LineQuantity;
}
