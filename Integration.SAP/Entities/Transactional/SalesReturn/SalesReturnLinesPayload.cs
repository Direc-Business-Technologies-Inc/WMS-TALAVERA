using Ardalis.GuardClauses;

namespace Integration.SAP.Entities.Transactional.SalesReturn;

/// <summary>
/// Line payload for Sales Return based on a source document (Delivery or Request).
/// BaseType: 15 = A/R Delivery
/// </summary>
public class SalesReturnLinesPayload
{
    public int BaseEntry { get; private set; }
    public int BaseType { get; private set; }
    public int BaseLine { get; private set; }
    public int LineNum { get; private set; }
    public string ItemCode { get; private set; }
    public string UoMCode { get; private set; }
    public decimal Quantity { get; private set; }
    public string WarehouseCode { get; private set; }

    public SalesReturnLinesPayload(
        int baseEntry,
        int baseType,
        int baseLine,
        int lineNum,
        string itemCode,
        string uomCode,
        decimal qty,
        string whsCode)
    {
        BaseEntry = Guard.Against.Null(baseEntry, nameof(BaseEntry));
        BaseType = Guard.Against.Null(baseType, nameof(BaseType));
        BaseLine = Guard.Against.Null(baseLine == -1 ? baseLine : baseLine - 1, nameof(BaseLine));
        LineNum = Guard.Against.Negative(lineNum, nameof(LineNum));
        ItemCode = Guard.Against.NullOrEmpty(itemCode, nameof(ItemCode));
        UoMCode = Guard.Against.NullOrEmpty(uomCode, nameof(UoMCode));
        Quantity = Guard.Against.NegativeOrZero(qty, nameof(Quantity));
        WarehouseCode = Guard.Against.NullOrEmpty(whsCode, nameof(WarehouseCode));
    }
}

public class StandaloneSalesReturnLinesPayload
{
    public int LineNum { get; private set; }
    public string ItemCode { get; private set; }
    public string UoMCode { get; private set; }
    public decimal Quantity { get; private set; }
    public string WarehouseCode { get; private set; }

    public StandaloneSalesReturnLinesPayload(
        int lineNum,
        string itemCode,
        string uomCode,
        decimal qty,
        string whsCode)
    {
        LineNum = Guard.Against.Negative(lineNum, nameof(LineNum));
        ItemCode = Guard.Against.NullOrEmpty(itemCode, nameof(ItemCode));
        UoMCode = Guard.Against.NullOrEmpty(uomCode, nameof(UoMCode));
        Quantity = Guard.Against.NegativeOrZero(qty, nameof(Quantity));
        WarehouseCode = Guard.Against.NullOrEmpty(whsCode, nameof(WarehouseCode));
    }
}
