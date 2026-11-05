namespace Application.DataTransferObjects.Transactions.Commons.NS.Request;

/// <summary>
///     Asks the API which vendor on a transfer order the scanned vendor barcode
///     belongs to. The payload is either the vendor's NetSuite internal id or the
///     vendor name as printed; either way the resolution is scoped to that
///     document's own line vendors.
/// </summary>
public class ResolveVendorRequestDTO
{
    public string OrderNumber { get; set; } = string.Empty;
    public string VendorBarcode { get; set; } = string.Empty;
}
