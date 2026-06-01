using Ardalis.GuardClauses;

namespace Domain.ValueObjects.Others;

public class UserNameVO : ValueObject
{
    public string Value { get; private set; }

    UserNameVO() { }

    public UserNameVO(string value) => Value = Guard.Against.NullOrEmpty(value, "Username", "User's Username cannot be null or empty");

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
