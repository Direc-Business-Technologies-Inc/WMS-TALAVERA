using Application.DataTransferObjects.Transactions.Commons.NS;

namespace Application.DataTransferObjects.Others.NS;

public class ItemBarcodesPerUoMDTO : InventoryItemDTO
{
    public string MaterialBarcode { get; set; }

    public string UoMName { get; set; }
    public int UoMRate { get; set; }
}
