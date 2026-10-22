namespace Shared.Libraries.ViewModel.TransferOrder;

/// <summary>
///     Vendor resolved from the scanned barcode printed under the STR's Vendor
///     field. The name is shown in the handheld header; the id is what the line
///     query filters on.
/// </summary>
public class VendorLookupVM
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
