using Ardalis.GuardClauses;
using Domain.Commons;
using Domain.Markers;

namespace Domain.Entities.Transaction.Common;

public class DocumentTypeDEM : EntityDEM, IAggregateRoot
{
    public int Code { get; private set; }
    public string Name { get; private set; }

    DocumentTypeDEM() { }

    DocumentTypeDEM(int code, string name)
    {
        Code = code;
        Name = name;
    }

    public static DocumentTypeDEM Create(int code, string name)
    {
        Guard.Against.NegativeOrZero(code, nameof(code), "Code cannot be negative or zero");
        Guard.Against.Expression(c => c < 100000, code, "Code must be greater than  100000", nameof(code));
        Guard.Against.NullOrEmpty(name, nameof(name), "Name cannot be null or empty");
        return new DocumentTypeDEM(code, name);
    }
}
