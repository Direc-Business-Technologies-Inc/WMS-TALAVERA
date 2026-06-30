using Application.DataTransferObjects.Others.NS;

namespace Application.DataTransferObjects.Transactions.InventoryCounting.NS;

public class InventoryCountingLineDTO : TransactionDTO
{
    public int? NetsuiteInventoryDetailInternalId { get; set; }
}
