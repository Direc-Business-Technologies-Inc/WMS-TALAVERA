using Ardalis.GuardClauses;
using Domain.ValueObjects;

namespace Domain.Entities.ValueObjects.Others;

public class WarehouseVO : ValueObject
{
    public string WhsCode { get; private set; }
    public string WhsName { get; private set; }

    public WarehouseVO() { }

    public WarehouseVO(string whsCode, string whsName)
    {
        WhsCode = Guard.Against.NullOrEmpty(whsCode, nameof(WhsCode), "Warehouse Code cannot be null or empty");
        WhsName = Guard.Against.NullOrEmpty(whsName, nameof(WhsName), "Warehouse Name cannot be null or empty");
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return WhsCode;
        yield return WhsName;
    }
}
