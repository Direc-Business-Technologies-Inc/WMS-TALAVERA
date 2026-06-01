namespace Domain.ValueObjects.Connectors;

public class ConnectorVO : ValueObject
{
    public Guid DocType { get; set; }
    public string LsmsDocNum { get; set; }

    ConnectorVO() { }

    public ConnectorVO(Guid docType, string docNum)
    {
        DocType = docType;
        LsmsDocNum = docNum;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DocType;
        yield return LsmsDocNum;
    }
}
