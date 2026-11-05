namespace Integration.NS.DataTransferObjects.SupplierReturn;

/// <summary>
/// Minimal source-line data used to validate the item set NetSuite will transform
/// from a purchase order into a Return to Supplier.
/// </summary>
public sealed class SupplierReturnPreferredVendorLineNSDTO
{
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public int? LineNumber { get; set; }
    public int? LocationId { get; set; }
    public int? VendorId { get; set; }
    public int? PurchaseOrderVendorId { get; set; }
    public int? PurchaseOrderSubsidiaryId { get; set; }
}
