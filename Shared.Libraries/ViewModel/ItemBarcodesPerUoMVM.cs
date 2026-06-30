using Shared.Libraries.ViewModel.Common;

namespace Shared.Libraries.ViewModel;

public class ItemBarcodesPerUoMVM : InventoryItemVM
{
    public string MaterialBarcode { get; set; } = string.Empty;

    public string UoMName { get; set; } = string.Empty;
    public int UoMRate { get; set; }

    public decimal? DefaultWeight;
}
