using Application.DataTransferObjects.Others.NS;

namespace Application.DataTransferObjects.Transactions.InventoryCounting.NS;

public class InventoryCountingLineDTO : TransactionDTO
{
    public int? NetsuiteInventoryDetailInternalId { get; set; }

    public int? BadInventoryDetailId { get; set; }

    public decimal? CountedQuantity { get; set; }

    public decimal GoodCountedQuantity { get; set; }

    public decimal BadCountedQuantity { get; set; }
}
