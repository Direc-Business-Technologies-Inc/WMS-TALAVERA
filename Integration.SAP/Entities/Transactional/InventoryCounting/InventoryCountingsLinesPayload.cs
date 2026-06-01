using Ardalis.GuardClauses;

namespace Integration.SAP.Entities.Transactional.InventoryCounting;

public class InventoryCountingsLinesPayload
{
    public string ItemCode { get; private set; }
    public string WarehouseCode { get; private set; }
    public string UoMCode { get; private set; }
    public decimal CountedQuantity { get; private set; }

    public InventoryCountingsLinesPayload(string itemCode, string whsCode, string uomCode, decimal ctdQty)
    {
        ItemCode = Guard.Against.NullOrEmpty(itemCode, nameof(ItemCode), "Item Code cant be null");
        WarehouseCode = Guard.Against.NullOrEmpty(whsCode, nameof(WarehouseCode), "Warehouse Code cant be null");
        UoMCode = Guard.Against.NullOrEmpty(uomCode, nameof(UoMCode), "UoM Code cant be null");
        CountedQuantity = Guard.Against.Negative(ctdQty, nameof(CountedQuantity), "Counted Quantity cannot be negative");
    }
}
