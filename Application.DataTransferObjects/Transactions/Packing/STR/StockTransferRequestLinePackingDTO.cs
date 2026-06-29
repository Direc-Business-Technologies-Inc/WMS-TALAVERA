using Application.DataTransferObjects.Others;

namespace Application.DataTransferObjects.Transactions.Packing.STR;

public class StockTransferRequestLinePackingDTO
{
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public ItemUnitDTO? UoM { get; set; } = null;
    public string Warehouse { get; set; } = string.Empty;
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityAlloted { get; set; }
}
