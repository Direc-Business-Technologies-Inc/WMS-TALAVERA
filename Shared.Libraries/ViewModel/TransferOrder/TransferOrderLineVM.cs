namespace Shared.Libraries.ViewModel.TransferOrder;

public class TransferOrderLineVM : TransactionVM
{
    public int NetsuiteFromLocationInternalId { get; set; }
    public int NetsuiteToLocationInternalId { get; set; }

    public int NetsuiteFromSubsidiaryInternalId { get; set; }
    public int NetsuiteSubsidiaryDefaultBOInternalId { get; set; }
    public int NetsuiteToSubsidiaryInternalId { get; set; }

    public int NetsuiteMaterialPrefferedBinId { get; set; }
    public decimal PreferredBinQuantityAvailableGood { get; set; }
    public decimal PreferredBinQuantityAvailableBad { get; set; }

    public int NetsuiteMaterialVendorAssignedBin { get; set; }
    public decimal VendorAssignedBinQuantityAvailableGood { get; set; }
    public decimal VendorAssignedBinQuantityAvailableBad { get; set; }

    // Per-line vendor requested on the STR (custcol_dbti_vendor). Nullable:
    // documents created before per-line vendors existed carry no value.
    public int? VendorId { get; set; }
    public string? VendorName { get; set; }

    public decimal GoodPerUomRate =>
                UoMRate == 0
                    ? 0
                    : !IsLocationUsedBin ? LocationItemQuantityAvailableGood
                    : (NetsuiteMaterialVendorAssignedBin != 0
                        ? VendorAssignedBinQuantityAvailableGood
                        : PreferredBinQuantityAvailableGood) / UoMRate;

    public decimal BadPerUomRate =>
                UoMRate == 0
                    ? 0
                    : !IsLocationUsedBin ? LocationItemQuantityAvailableBad
                    : (NetsuiteMaterialVendorAssignedBin != 0
                        ? VendorAssignedBinQuantityAvailableBad
                        : PreferredBinQuantityAvailableBad) / UoMRate;

    // NetSuite 'T'/'F' closed flag on the transaction line (mirrors
    // LocationUsedBin, which keeps the raw 'T'/'F' string rather than a bool).
    public string IsClosed { get; set; } = string.Empty;

    // NetSuite-maintained cumulative shipped/received quantity
    // (transactionline.quantityshiprecv). On a partially fulfilled order the
    // open quantity is LineQuantity - QuantityAlreadyShipped, NOT the
    // quantitypacked-based NSLineQuantityPacked.
    public decimal QuantityAlreadyShipped { get; set; }

    public decimal NSLineQuantityRemaining => LineQuantity - QuantityAlreadyShipped;

    public bool IsLineClosed => string.Equals(IsClosed, "T", StringComparison.OrdinalIgnoreCase);

    // Closed or fully shipped: no remaining quantity to pack, so the line is
    // routed to the read-only DONE list and never scanned.
    public bool IsCompleted => IsLineClosed || NSLineQuantityRemaining <= 0;
}
