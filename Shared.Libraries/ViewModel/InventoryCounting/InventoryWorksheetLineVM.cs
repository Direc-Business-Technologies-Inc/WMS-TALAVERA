using Shared.Libraries.ViewModel.Common;

namespace Shared.Libraries.ViewModel.InventoryCounting;

public class InventoryWorksheetLineVM : InventoryItemVM
{
    public int? NetsuiteInventoryDetailInternalId { get; set; }

    public decimal GoodScannedQuantity { get; set; }
    public decimal BadScannedQuantity { get; set; }

    public int ScanCount { get; set; }
}
