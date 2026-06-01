using Ardalis.GuardClauses;

namespace Integration.SAP.Entities.Transactional.GoodsReturn;

public class PurchaseReturnPayload
{
    public DateTime DocDate { get; private set; }
    public DateTime DocDueDate { get; private set; }
    public string CardCode { get; private set; }
    public string U_RetType { get; private set; }
    public string U_PrepBy { get; private set; }
    public string Comments { get; private set; }
    public string? U_SchlYear { get; private set; }
    public string? U_DRNo { get; private set; }
    public string? U_SINo { get; private set; }
    public string? U_DelBy { get; private set; }
    public string? U_RecBy { get; private set; }
    public string? U_Remarks { get; private set; }
    public string? U_AppBy { get; private set; }
    public string? U_CheckBy { get; private set; }
    public string? U_RevBy { get; private set; }


    public List<object> DocumentLines { get; private set; } = [];

    public PurchaseReturnPayload(DateTime docDate,
                       DateTime docDueDate,
                       string cardCode,
                       string returnType,
                       string prepBy,
                       List<object> documentLines,
                       string? schoolYear = null,
                       string? drNo = null,
                       string? siNo = null,
                       string? deliveredBy = null,
                       string? receivedBy = null,
                       string? docRemarks = null,
                       string? approvedBy = null,
                       string? reviewedBy = null,
                       string? checkedBy = null)
    {
        DocDate = Guard.Against.NullOrOutOfSQLDateRange(docDate, nameof(DocDate), "Doc Date cannot be null or out of range");
        DocDueDate = Guard.Against.NullOrOutOfSQLDateRange(docDueDate, nameof(DocDueDate), "Doc Due Date cannot be null or out of range");
        CardCode = Guard.Against.NullOrEmpty(cardCode, nameof(CardCode), "Card Code cannot be null or empty");
        U_RetType = Guard.Against.NullOrEmpty(returnType, nameof(U_RetType), "Return Type cannot be null or empty");
        U_PrepBy = Guard.Against.NullOrEmpty(prepBy, nameof(U_PrepBy), "Prepared By cannot be null or empty");
        Comments = "Posted from WMS";
        U_SchlYear = schoolYear;
        U_DRNo = drNo;
        U_SINo = siNo;
        U_DelBy = deliveredBy;
        U_RecBy = receivedBy;
        U_Remarks = docRemarks;
        U_AppBy = approvedBy;
        U_CheckBy = checkedBy;
        U_RevBy = reviewedBy;

        DocumentLines = [.. Guard.Against.NullOrEmpty(documentLines, nameof(DocumentLines), "Document Lines cannot be null or empty")];
    }
}
