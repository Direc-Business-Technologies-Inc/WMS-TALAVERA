using Ardalis.GuardClauses;

namespace Integration.SAP.Entities.Transactional.Receiving;

public class GoodsReceiptPOPayload
{
    public string CardCode { get; private set; }
    public DateTime DocDate { get; private set; }
    public DateTime DocDueDate { get; private set; }
    public DateTime TaxDate { get; private set; }
    public string Comments { get; private set; }
    public string? U_SchlYear { get; private set; }
    public string? U_PONo { get; private set; }
    public string? U_DRNo { get; private set; }
    public int? U_Time { get; private set; }
    public string? U_SINo { get; private set; }
    public string? U_PurchType { get; private set; }
    public string? U_ItemName { get; private set; }
    public string? U_DelBy { get; private set; }
    public string? U_RecBy { get; private set; }
    public string? U_Remarks { get; private set; }
    public string? U_PrepBy { get; private set; }
    public string? U_RevBy { get; private set; }
    public string? U_AppBy { get; private set; }
    public string? U_NotedBy { get; private set; }
    public List<object> DocumentLines { get; private set; } = [];

    public GoodsReceiptPOPayload(string cardCode,
                          DateTime docDate,
                          DateTime docDueDate,
                          DateTime taxDate,
                          string prepBy,
                          List<object> documentLines,
                          string? schlYear = null,
                          string? poNo = null,
                          string? drNo = null,
                          int? time = null,
                          string? siNo = null,
                          string? purchType = null,
                          string? itemName = null,
                          string? delBy = null,
                          string? recBy = null,
                          string? docRemarks = null,
                          string? revBy = null,
                          string? appBy = null,
                          string? notedBy = null)
    {
        CardCode = Guard.Against.NullOrEmpty(cardCode, nameof(CardCode), "Card Code cannot be null or empty");
        DocDate = Guard.Against.NullOrOutOfSQLDateRange(docDate, nameof(DocDate), "Doc Date cannot be null or out of range");
        DocDueDate = Guard.Against.NullOrOutOfSQLDateRange(docDueDate, nameof(DocDueDate), "Doc Due Date cannot be null or out of range");
        TaxDate = Guard.Against.NullOrOutOfSQLDateRange(taxDate, nameof(TaxDate), "Tax Date cannot be null or out of range");
        U_PrepBy = Guard.Against.NullOrEmpty(prepBy, nameof(U_PrepBy), "Prepared By cannot be null or empty");
        DocumentLines = [.. Guard.Against.NullOrEmpty(documentLines, nameof(DocumentLines), "Document Lines cannot be null or empty")];
        Comments = "Posted from WMS";
        U_SchlYear = schlYear;
        U_PONo = poNo;
        U_DRNo = drNo;
        U_Time = time;
        U_SINo = siNo;
        U_DelBy = delBy;
        U_RecBy = recBy;
        U_Remarks = docRemarks;
        U_PrepBy = prepBy;
        U_RevBy = revBy;
        U_AppBy = appBy;
        U_NotedBy = notedBy;
        U_Time = time;
        U_PurchType = purchType;
        U_ItemName = itemName;
    }
}
