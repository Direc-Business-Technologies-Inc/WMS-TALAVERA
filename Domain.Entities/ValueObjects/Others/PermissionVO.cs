
using Ardalis.GuardClauses;

namespace Domain.ValueObjects.Others;

public class PermissionVO : ValueObject
{
    public string Value { get; private set; }

    public PermissionVO() { }

    public PermissionVO(string value)
    {
        Value = Guard.Against.NullOrEmpty(value, nameof(PermissionVO.Value), "Permission value cannot be null or empty");
    }

    public void SetValue(string value)
    {
        Value = Guard.Against.NullOrEmpty(value, nameof(PermissionVO.Value), "Permission value cannot be null or empty");
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
