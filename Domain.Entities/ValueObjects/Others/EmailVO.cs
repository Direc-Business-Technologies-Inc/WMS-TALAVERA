
using Ardalis.GuardClauses;

namespace Domain.ValueObjects.Others;

public class EmailVO(string address) : ValueObject
{
    public string Address { get; private set; } = Guard.Against.NullOrEmpty(address, "Email", "User email cannot be null or empty");


    public void SetValue(string address)
    {
        if (address.Contains("Admin")) throw new InvalidOperationException("Admin emails are not allowed.");
        Address = Guard.Against.NullOrEmpty(address, "Email", "User email cannot be null or empty");
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Address;
    }
}
