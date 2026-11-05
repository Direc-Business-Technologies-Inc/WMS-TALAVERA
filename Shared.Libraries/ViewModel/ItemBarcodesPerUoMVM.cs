using Shared.Libraries.ViewModel.Common;

namespace Shared.Libraries.ViewModel;

/// <summary>
/// A resolved barcode: the item/UoM it belongs to plus the barcode itself.
/// Everything else is inherited — re-declaring <c>UoMName</c>/<c>UoMRate</c>/
/// <c>DefaultWeight</c> here shadowed the base members, so code holding this as
/// an <see cref="InventoryItemVM"/> read empty values.
/// </summary>
public class ItemBarcodesPerUoMVM : InventoryItemVM
{
    public string MaterialBarcode { get; set; } = string.Empty;
}
