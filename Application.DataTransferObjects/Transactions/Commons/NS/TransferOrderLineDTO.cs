using Application.DataTransferObjects.Others.NS;

namespace Application.DataTransferObjects.Transactions.Commons.NS;

public class TransferOrderLineDTO : TransactionDTO
{
    public int TransferCategory { get; set; }

    public int NetsuiteFromLocationInternalId { get; set; }
    public int NetsuiteToLocationInternalId { get; set; }

    public int NetsuiteFromSubsidiaryInternalId { get; set; }
    public int NetsuiteSubsidiaryDefaultBOInternalId { get; set; }
    public int NetsuitePrefferedBadBinId { get; set; }
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

    // NetSuite 'T'/'F' flag on the transaction line. A closed line is fully
    // shipped (or closed outright) and is NOT present on the
    // !transform/itemFulfillment item sublist, so posting it is an invalid
    // static-sublist insert.
    public string IsClosed { get; set; } = string.Empty;

    // NetSuite-maintained cumulative shipped/received quantity on the line
    // (transactionline.quantityshiprecv). The remaining open quantity is
    // LineQuantity - QuantityAlreadyShipped; it is the correct baseline on a
    // partially fulfilled order, unlike quantitypacked.
    public decimal QuantityAlreadyShipped { get; set; }
}
