using Application.DataTransferObjects.Others.NS;

namespace Application.DataTransferObjects.Transactions.InventoryCounting.NS;

public class PatchInventoryCountingDTO : TransactionDTO
{
    public int? NetsuiteInventoryDetailInternalId { get; set; }

    public decimal ScannedQuantity { get; set; }

    public bool IsBad { get; set; }
}
