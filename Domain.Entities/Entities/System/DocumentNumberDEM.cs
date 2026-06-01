
using Ardalis.GuardClauses;
using Domain.Commons;

namespace Domain.Entities.System;

public class DocumentNumberDEM : EntityDEM
{
    public Guid DocumentTypeId {  get; private set; }
    public string Code { get; private set; }
    public string Prefix { get; private set; }
    public int CurrentNumber { get; private set; }
    public int NextNumber { get; private set; }

    protected DocumentNumberDEM() { }

    protected DocumentNumberDEM(Guid documentTypeId, string code, string prefix)
    {
        DocumentTypeId = Guard.Against.NullOrEmpty(documentTypeId, nameof(DocumentTypeId), "Document Type Id cannot be nullor empty");
        Code = Guard.Against.NullOrEmpty(code, nameof(Code), "Code cannot be nullor empty");
        Prefix = Guard.Against.NullOrEmpty(prefix, nameof(Prefix), "Prefix cannot be nullor empty");
        CurrentNumber = 0;
        NextNumber = 1;
    }

    public static DocumentNumberDEM Create(Guid documentTypeId, string code, string prefix)
    {
        return new DocumentNumberDEM(documentTypeId, code, prefix);
    }

    public DocumentNumberDEM Update(string code, string prefix)
    {
        Code = code;
        Prefix = prefix;

        return this;
    }

    public DocumentNumberDEM IncrementNumber()
    {
        CurrentNumber = NextNumber;
        NextNumber += 1;

        return this;
    }

    public string GenerateCurrentDocNum()
    {
        string zeroes = GenerateZeroes(4 - CurrentNumber.ToString().Length);
        string nextMacId = $"{Code}{Prefix}-{zeroes}{CurrentNumber}";

        return nextMacId;
    }

    public string GenerateNextDocNum()
    {
        string zeroes = GenerateZeroes(4 - NextNumber.ToString().Length);
        string nextMacId = $"{Code}{Prefix}-{zeroes}{NextNumber}";

        return nextMacId;
    }

    static string GenerateZeroes(int count)
    {
        if (count == 0) return "";
        string z = "";
        for (int i = 0; i < count; i++)
        {
            z = $"{z}0";
        }
        return z;
    }
}
