
using Ardalis.GuardClauses;

namespace Domain.ValueObjects.Others;

public class PersonNameVO : ValueObject
{
    public string FirstName { get; private set; }
    public string? MiddleName { get; private set; }
    public string LastName { get; private set; }

    public string FullName => string.IsNullOrEmpty(MiddleName)
        ? string.IsNullOrEmpty(LastName)
            ? FirstName
            : $"{FirstName} {LastName}"
        : string.IsNullOrEmpty(LastName)
            ? $"{FirstName} {MiddleName.ToUpperInvariant()[0]}." 
            : $"{FirstName} {MiddleName.ToUpperInvariant()[0]}. {LastName}";

    PersonNameVO() { }

    public PersonNameVO(string firstName, string? middleName, string lastName)
    {
        FirstName = Guard.Against.Null(firstName, nameof(firstName), "First name cannot be null or empty");
        MiddleName = middleName;
        LastName = Guard.Against.Null(lastName, nameof(lastName), "Last name cannot be null or empty");
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return FirstName;
        yield return MiddleName ?? string.Empty;
        yield return LastName ?? string.Empty;
    }

    string GetFull() => $"{FirstName}{(string.IsNullOrEmpty(MiddleName) ? "" : $"{MiddleName}")}{(string.IsNullOrEmpty(LastName) ? "" : $" {LastName}")}";
    string GetFormal() => $"{FirstName}{(string.IsNullOrEmpty(MiddleName) ? "" : $" {MiddleName.ToUpperInvariant()[0]}.")}{(string.IsNullOrEmpty(LastName) ? "" : $" {LastName}")}";
    string GetLastFirst() => $"{(string.IsNullOrEmpty(LastName) ? "" : $"{LastName}, ")}{FirstName}";
    public string GetFirstLast() => $"{FirstName}{(string.IsNullOrEmpty(LastName) ? "" : $" {LastName}")}";
    string GetFirstOnly() => FirstName;
    string GetLastOnly() => LastName ?? string.Empty;
    string GetInitials() => 
        $"{FirstName.ToUpperInvariant()[0]}" +
        $"{(string.IsNullOrEmpty(MiddleName) ? "" : $".{MiddleName.ToUpperInvariant()[0]}")}" +
        $"{(string.IsNullOrEmpty(LastName) ? "" : $".{LastName.ToUpperInvariant()[0]}")}";

    public override string ToString() => ToString(NameFormat.Full);

    public string ToString(NameFormat format)
    {
        return format switch
        {
            NameFormat.Full => GetFull(),
            NameFormat.Formal => GetFormal(),
            NameFormat.LastFirst => GetLastFirst(),
            NameFormat.FirstLast => GetFirstLast(),
            NameFormat.FirstOnly => GetFirstOnly(),
            NameFormat.LastOnly => GetLastOnly(),
            NameFormat.Initials => GetInitials(),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }
}

public enum NameFormat
{
    Full, // CMH : 08/13/2025 : Ian Nicolas Antonio, Ian Nicolas, Ian Anntonio, Ian (Will depend on what are the values of the middle and last names)
    Formal, // CMH : 08/13/2025 : Ian N. Antonio, Ian Nicolas (when last name is empty), Ian Antonio (when middle is empty), Ian (when middle and last names are empty)
    LastFirst, // CMH : 08/13/2025 : Antonio, Ian, Ian (when last is empty)
    FirstLast, // CMH : 08/13/2025 : Ian Antonio, Ian (when last is empy)
    FirstOnly, // CMH : 08/13/2025 : Ian
    LastOnly, // CMH : 08/13/2025 : Antonio, string.Empty (when no last name is given)
    Initials // CMH : 08/13/2025 : I.N.A || I.A || Ian (when there's no last name given)
}