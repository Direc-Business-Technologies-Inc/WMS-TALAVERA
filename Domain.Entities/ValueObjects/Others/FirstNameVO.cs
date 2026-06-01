
namespace Domain.ValueObjects.Others;

public class FirstNameVO : ValueObject
{
    public string Value { get; private set; }

    FirstNameVO() { }

    public FirstNameVO(string firstName)
    {
        Value = firstName ?? throw new ArgumentNullException(nameof(firstName));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
