using Ardalis.GuardClauses;
using Domain.ValueObjects;

namespace Domain.Entities.ValueObjects.Others;

public class BusinessPartnerVO : ValueObject
{
    public string CardCode { get; private set; }
    public string CardName { get; private set; }

    public BusinessPartnerVO() { }

    public BusinessPartnerVO(string cardCode, string cardName)
    {
        CardCode = Guard.Against.NullOrEmpty(cardCode, nameof(CardCode), "Business Partner Card Code cannot be null or empty");
        CardName = Guard.Against.NullOrEmpty(cardName, nameof(CardName), "Business Partner Card Name cannot be null or empty");
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        throw new NotImplementedException();
    }
}
