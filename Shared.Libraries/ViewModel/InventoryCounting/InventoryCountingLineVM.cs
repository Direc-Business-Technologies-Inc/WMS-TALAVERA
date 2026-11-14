namespace Shared.Libraries.ViewModel.InventoryCounting;

public class InventoryCountingLineVM : TransactionVM
{
    public int? NetsuiteInventoryDetailInternalId { get; set; }

    public int? BadInventoryDetailId { get; set; }

    public decimal? CountedQuantity { get; set; }

    public decimal GoodCountedQuantity { get; set; }

    public decimal BadCountedQuantity { get; set; }

    public bool IsCounted { get; set; }
}
