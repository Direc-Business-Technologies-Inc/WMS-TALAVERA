using Ardalis.GuardClauses;

namespace Domain.ValueObjects.Others;

public class ModulePermissionVO : ValueObject
{
    public Guid Value { get; private set; }

    public ModulePermissionVO() { }

    public ModulePermissionVO(Guid value)
    {
        Value = Guard.Against.NullOrEmpty(value, nameof(ModulePermissionVO.Value), "Permission value cannot be null or empty");
    }

    public void SetValue(Guid value)
    {
        Value = Guard.Against.NullOrEmpty(value, nameof(ModulePermissionVO.Value), "Permission value cannot be null or empty");
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
