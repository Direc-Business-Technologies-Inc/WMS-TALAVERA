using Ardalis.GuardClauses;

namespace Integration.SAP.Entities.Transactional.SalesReturn;

public class SalesReturnPayload
{
    public DateTime DocDate { get; private set; }
    public DateTime DocDueDate { get; private set; }
    public string CardCode { get; private set; }
    public string? U_RetType { get; private set; }
    public string U_PrepBy { get; private set; }
    public string Comments { get; private set; }
    public string? U_SchlYear { get; private set; }
    public string? U_DRNo { get; private set; }
    public string? U_SINo { get; private set; }
    public string? U_PURNo { get; private set; }
    public string? U_SONo { get; private set; }
    public string? U_Designation { get; private set; }
    public string? U_ReturnedBy { get; private set; }
    public string? U_PickBy { get; private set; }
    public string? U_Remarks { get; private set; }
    public string? U_CheckBy { get; private set; }
    public string? U_NotedBy { get; private set; }
    public string? U_AppBy { get; private set; }

    public List<object> DocumentLines { get; private set; } = [];

    public SalesReturnPayload(
        DateTime docDate,
        DateTime docDueDate,
        string cardCode,
        string prepBy,
        List<object> documentLines,
        string? returnType = null,
        string? schoolYear = null,
        string? drNo = null,
        string? siNo = null,
        string? purNo = null,
        string? soNo = null,
        string? designation = null,
        string? returnedBy = null,
        string? pickBy = null,
        string? docRemarks = null,
        string? checkedBy = null,
        string? notedBy = null,
        string? approvedBy = null)
    {
        DocDate = Guard.Against.NullOrOutOfSQLDateRange(docDate, nameof(DocDate));
        DocDueDate = Guard.Against.NullOrOutOfSQLDateRange(docDueDate, nameof(DocDueDate));
        CardCode = Guard.Against.NullOrEmpty(cardCode, nameof(CardCode));
        U_PrepBy = Guard.Against.NullOrEmpty(prepBy, nameof(U_PrepBy));
        Comments = "Posted from WMS";
        U_RetType = returnType;
        U_SchlYear = schoolYear;
        U_DRNo = drNo;
        U_SINo = siNo;
        U_PURNo = purNo;
        U_SONo = soNo;
        U_Designation = designation;
        U_ReturnedBy = returnedBy;
        U_PickBy = pickBy;
        U_Remarks = docRemarks;
        U_CheckBy = checkedBy;
        U_NotedBy = notedBy;
        U_AppBy = approvedBy;

        DocumentLines = [.. Guard.Against.NullOrEmpty(documentLines, nameof(DocumentLines))];
    }
}
