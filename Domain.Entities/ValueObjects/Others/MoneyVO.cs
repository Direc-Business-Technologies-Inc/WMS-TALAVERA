
using Ardalis.GuardClauses;
using Domain.Enums.Commons;

namespace Domain.ValueObjects.Others;

public class MoneyVO : ValueObject
{
    public decimal Amount { get; private set; }
    public Currency Currency { get; private set; }

    MoneyVO() { }

    public MoneyVO(decimal amount) : this(amount, Currency.PHP)
    {
        Amount = amount;
    }

    public MoneyVO(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = Guard.Against.EnumOutOfRange(currency);
    }

    public void AddAmount(decimal amount)
    {
        Amount += amount;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
