using Ardalis.GuardClauses;

namespace Integration.SAP.Entities.Transactional.Delivery;

public class DeliveryNotesLinesPayload
{
    public int BaseEntry { get; private set; }
    public int BaseType { get; private set; }
    public int BaseLine { get; private set; }
    public int LineNum { get; private set; }
    public string ItemCode { get; private set; }
    public decimal Quantity { get; private set; }
    public string WarehouseCode { get; private set; }

    public DeliveryNotesLinesPayload(int baseEntry,
                                     int baseType,
                                     int baseLine,
                                     int lineNum,
                                     string itemCode,
                                     decimal quantity,
                                     string warehouseCode)
    {
        BaseEntry = Guard.Against.Negative(baseEntry, nameof(BaseEntry), "Base Entry cannot be negative");
        BaseType = Guard.Against.Negative(baseType, nameof(BaseType), "Base Type cannot be negative");
        BaseLine = Guard.Against.Negative(baseLine - 1, nameof(BaseLine), "Base Line cannot be negative");
        LineNum = Guard.Against.Negative(lineNum, nameof(LineNum), "Line Num cannot be negative");
        ItemCode = Guard.Against.NullOrEmpty(itemCode, nameof(ItemCode), "Item Code cannot be null or empty");
        Quantity = Guard.Against.NegativeOrZero(quantity, nameof(Quantity), "Quantity cannot be negative or zero");
        WarehouseCode = Guard.Against.NullOrEmpty(warehouseCode, nameof(WarehouseCode), "Warehouse Code cannot be null or empty");
    }
}
