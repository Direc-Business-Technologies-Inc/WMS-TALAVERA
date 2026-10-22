namespace Application.DataTransferObjects.Transactions.Commons.NS;

/// <summary>
///     A vendor resolved from the barcode printed under the STR's Vendor field.
///     The barcode carries the vendor NAME — not an internal id — so the name is
///     resolved against the vendors actually present on the transfer order, and
///     the id is read from that line data afterwards.
/// </summary>
public class VendorLookupDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
