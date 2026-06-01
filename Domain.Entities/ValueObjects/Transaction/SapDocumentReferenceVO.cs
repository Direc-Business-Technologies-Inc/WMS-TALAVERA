
using Ardalis.GuardClauses;

namespace Domain.ValueObjects.Transaction;

public class SapDocumentReferenceVO : ValueObject
{
    public int? DocEntry { get; private set; }
    public int? DocNum { get; private set; }
    public int? BaseDocEntry { get; private set; }
    public int? BaseDocNum { get; private set; }

    SapDocumentReferenceVO() { }

    public SapDocumentReferenceVO(int? docEntry = null, int? docNum = null, int? baseDocEntry = null, int? baseDocNum = null)
    {
        DocEntry = docEntry;
        DocNum = docNum;
        BaseDocEntry = baseDocEntry;
        BaseDocNum = baseDocNum;
    }

    public SapDocumentReferenceVO SetDocEntry(int docEntry)
    {
        DocEntry = Guard.Against.Null(docEntry, nameof(docEntry), "Document Entry cannot be null");
        return this;
    }
    
    public SapDocumentReferenceVO SetBaseDocEntry(int baseDocEntry)
    {
        BaseDocEntry = Guard.Against.Null(baseDocEntry, nameof(baseDocEntry), "Base Document Entry cannot be null");
        return this;
    }

    public SapDocumentReferenceVO SetDocNum(int docNum)
    {
        DocNum = Guard.Against.Null(docNum, nameof(docNum), "Document Number cannot be null");
        return this;
    }

    public SapDocumentReferenceVO SetBaseDocNum(int baseDocNum)
    {
        BaseDocNum = Guard.Against.Null(baseDocNum, nameof(baseDocNum), "Base Document Number cannot be null");
        return this;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DocEntry;
        yield return DocNum;
        yield return BaseDocEntry;
        yield return BaseDocNum;
    }

}
