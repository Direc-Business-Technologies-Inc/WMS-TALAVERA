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

    // The quantity this line may still be packed, in the SAME unit the scanner
    // accumulates into ScannedQuantity (the line's display UoM - a barcode
    // contributes barcodeRate / lineRate). LineQuantity and QuantityAlreadyShipped
    // are raw stock units (transactionline.quantity), so the display-unit figure
    // must divide by the line's UoM conversion rate.
    //
    // Root cause of the 2026-10-23 over-scan report (STR-THM00100000867,
    // item 00032678): the scan guard compared display-unit scans against
    // NSLineQuantityRemaining (stock units), so a BOX12 line allowed 12x its
    // remaining quantity - 888 scans on a line planned for 74 boxes.
    //
    // max(packed, shipped) keeps the plan honest on both sides: a line already
    // packed has nothing left to pack, while a stale/zero quantitypacked cannot
    // re-open a line that was already shipped. Backordered quantity is
    // unavailable stock and is not packable (the white box always showed it
    // that way); on a fully backordered SHIPPING line this is 0, so the line
    // refuses every scan instead of allowing rate x quantity.
    public decimal PackableQuantity
    {
        get
        {
            var rate = UoMRate > 0 ? UoMRate : 1m;
            var committed = Math.Max(LineQuantityPacked, QuantityAlreadyShipped);
            var remaining = LineQuantity - committed - LineQuantityBackOrdered;
            return remaining > 0 ? remaining / rate : 0m;
        }
    }

    // Green state and the scan guard must agree: the line is packed to plan
    // once the scanned quantity reaches the packable quantity (>=, so a line
    // planned at 0 is immediately "done" and never scannable).
    public bool PackedToPlan => ScannedQuantity >= PackableQuantity;

    public bool IsLineClosed => string.Equals(IsClosed, "T", StringComparison.OrdinalIgnoreCase);

    // Closed or fully shipped: no remaining quantity to pack, so the line is
    // routed to the read-only DONE list and never scanned.
    public bool IsCompleted => IsLineClosed || NSLineQuantityRemaining <= 0;
}
