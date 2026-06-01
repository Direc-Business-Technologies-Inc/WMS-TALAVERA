using Ardalis.GuardClauses;

namespace Integration.SAP.Entities.Transactional.Receiving;

public interface IGoodsReceiptPOLine { }

public class GoodsReceiptPOLinesStandalonePayload : GoodsReceiptPOLinesBasePayload, IGoodsReceiptPOLine
{
    public GoodsReceiptPOLinesStandalonePayload(
        int lineNum,
        string itemCode,
        decimal quantity,
        decimal unitCost,
        string taxCode,
        string warehouseCode,
        string inputType)
        : base(lineNum, itemCode, quantity, unitCost, taxCode, warehouseCode, inputType)
    {
    }
}

public class GoodsReceiptPOLinesPayload : GoodsReceiptPOLinesBasePayload, IGoodsReceiptPOLine
{
    public int BaseEntry { get; private set; }
    public int BaseType { get; private set; }
    public int BaseLine { get; private set; }

    public GoodsReceiptPOLinesPayload(int baseEntry,
                               int baseType,
                               int baseLine,
                               int lineNum,
                               string itemCode,
                               decimal qty,
                               decimal unitCost,
                               string taxCode,
                               string whsCode,
                               string inputType) : base(lineNum, itemCode, qty, unitCost, taxCode, whsCode, inputType)
    {
        BaseEntry = Guard.Against.Negative(baseEntry, nameof(BaseEntry), "Base Entry cannot be negative or null");
        BaseType = Guard.Against.Negative(baseType, nameof(BaseType), "Base Type cannot be negative or null");
        BaseLine = Guard.Against.Negative(baseLine == 0 ? baseLine : baseLine - 1, nameof(BaseLine), "Base Line cannot be negative or null");
    }
}

public abstract class GoodsReceiptPOLinesBasePayload
{
    public int LineNum { get; private set; }
    public string ItemCode { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public string TaxCode { get; private set; }
    public string WarehouseCode { get; private set; }
    public string U_InputType { get; private set; }

    public GoodsReceiptPOLinesBasePayload(int lineNum,
                               string itemCode,
                               decimal qty,
                               decimal unitCost,
                               string taxCode,
                               string whsCode,
                               string inputType)
    {
        LineNum = Guard.Against.Negative(lineNum, nameof(LineNum), "Base Line cannot be negative or null");
        ItemCode = Guard.Against.NullOrEmpty(itemCode, nameof(ItemCode), "Item Code cannot be null or empty");
        Quantity = Guard.Against.NegativeOrZero(qty, nameof(Quantity), "Quantity cannot be negative or zero");
        UnitCost = Guard.Against.NegativeOrZero(qty, nameof(UnitCost), "Unit Cost cannot be negative or zero");
        TaxCode = Guard.Against.NullOrEmpty(taxCode, nameof(TaxCode), "Tax Code cannot be null or empty");
        WarehouseCode = Guard.Against.NullOrEmpty(whsCode, nameof(WarehouseCode), "Warehouse Code cannot be null or empty");
        U_InputType = Guard.Against.NullOrEmpty(inputType, nameof(U_InputType), "Input Type cannot be null or empty");
    }
}