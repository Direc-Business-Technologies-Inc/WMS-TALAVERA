using Ardalis.GuardClauses;
using System.Reflection.Emit;

namespace Integration.SAP.Entities.Transactional.GoodsReturn;

public class PurchaseReturnLinesPayload
{
    public int BaseEntry { get; private set; }
    public int BaseType { get; private set; }
    public int BaseLine { get; private set; }
    public int LineNum { get; private set; }
    public string ItemCode { get; private set; }
    public string UoMCode { get; private set; }
    public decimal Quantity { get; private set; }
    public string WarehouseCode { get; private set; }

    public PurchaseReturnLinesPayload(int baseEntry,
                           int baseType,
                           int baseLine,
                           int lineNum,
                           string itemCode,
                           string uomCode,
                           decimal qty,
                           string whsCode)
    {
        BaseEntry = Guard.Against.Null(baseEntry, nameof(BaseEntry), "Base Entry cannot be negative or null");
        BaseType = Guard.Against.Null(baseType, nameof(BaseType), "Base Type cannot be null");
        BaseLine = Guard.Against.Null(baseLine == -1 ? baseLine : baseLine - 1, nameof(BaseLine), "Base Line cannot be negative or null");
        LineNum = Guard.Against.Negative(lineNum, nameof(LineNum), "Base Line cannot be negative or null");
        ItemCode = Guard.Against.NullOrEmpty(itemCode, nameof(ItemCode), "Item Code cannot be null or empty");
        UoMCode = Guard.Against.NullOrEmpty(uomCode, nameof(UoMCode), "UoM Code cannot be null or empty");
        Quantity = Guard.Against.NegativeOrZero(qty, nameof(Quantity), "Quantity cannot be negative or zero");
        WarehouseCode = Guard.Against.NullOrEmpty(whsCode, nameof(WarehouseCode), "Warehouse Code cannot be null or empty");
    }
}

public class StandalonePurchaseReturnLinesPayload
{
    public int LineNum { get; private set; }
    public string ItemCode { get; private set; }
    public decimal Quantity { get; private set; }
    public string UoMCode { get; private set; }
    public string WarehouseCode { get; private set; }

    public StandalonePurchaseReturnLinesPayload(
                           int lineNum,
                           string itemCode,
                           string uomCode,
                           decimal qty,
                           string whsCode)
    {
        LineNum = Guard.Against.Negative(lineNum, nameof(LineNum), "Base Line cannot be negative or null");
        ItemCode = Guard.Against.NullOrEmpty(itemCode, nameof(ItemCode), "Item Code cannot be null or empty");
        UoMCode = Guard.Against.NullOrEmpty(uomCode, nameof(UoMCode), "UoM Code cannot be null or empty");
        Quantity = Guard.Against.NegativeOrZero(qty, nameof(Quantity), "Quantity cannot be negative or zero");
        WarehouseCode = Guard.Against.NullOrEmpty(whsCode, nameof(WarehouseCode), "Warehouse Code cannot be null or empty");
    }
}
