using Application.DataTransferObjects.Transactions.Commons.NS;

namespace Application.DataTransferObjects.Transactions.InventoryCounting.NS;

public class InventoryWorksheetLineDTO : InventoryItemDTO
{
    public int? NetsuiteInventoryDetailInternalId { get; set; }

    public decimal GoodScannedQuantity { get; set; }
    public decimal BadScannedQuantity { get; set; }

    public int NetsuiteBinInternalId { get; set; }

    public int ScanCount { get; set; }
}
