using Ardalis.GuardClauses;

namespace Integration.SAP.Entities.Transactional.Delivery;

public class DeliveryNotesPayload
{
    public string CardCode { get; private set; }
    public DateTime DocDate { get; private set; }
    public DateTime DocDueDate { get; private set; }
    public string Comments { get; private set; }
    public string U_PrepBy { get; private set; }
    public string? U_SchlYear { get; private set; }
    public string? U_DRNo { get; private set; }
    public string? U_DelivMeans { get; private set; }
    public string? U_Courier { get; private set; }
    public string? U_CourName { get; private set; }
    public DateTime? U_ActualDelivDate { get; private set; }
    public string? U_Desig { get; private set; }
    public string? U_WBNo { get; private set; }
    public string? U_PlateNo { get; private set; }
    public string? U_Driver { get; private set; }
    public string? U_Remarks { get; private set; }
    public string? U_RecBy { get; private set; }
    public string? U_AppBy { get; private set; }
    public string? U_NotedBy { get; private set; }
    public IEnumerable<DeliveryNotesLinesPayload> DocumentLines { get; private set; }

    public DeliveryNotesPayload(string cardCode,
                                DateTime docDate,
                                DateTime docDueDate,
                                string prepBy,
                                IEnumerable<DeliveryNotesLinesPayload> documentLines,
                                string? schlYear = null,
                                string? drNo = null,
                                string? delivMeans = null,
                                string? courier = null,
                                string? courName = null,
                                DateTime? actualDelivDate = null,
                                string? desig = null,
                                string? wbNo = null,
                                string? plateNo = null,
                                string? driver = null,
                                string? docRemarks = null,
                                string? recBy = null,
                                string? appBy = null,
                                string? notedBy = null)
    {
        CardCode = Guard.Against.NullOrEmpty(cardCode, nameof(CardCode), "Card Code cannot be null or empty");
        DocDate = Guard.Against.NullOrOutOfSQLDateRange(docDate, nameof(DocDate), "Doc Date cannot be null or out of range");
        DocDueDate = Guard.Against.NullOrOutOfSQLDateRange(docDueDate, nameof(DocDueDate), "Doc Due Date cannot be null or out of range");
        U_PrepBy = Guard.Against.NullOrEmpty(prepBy, nameof(U_PrepBy), "Prepared By cannot be null or empty");
        DocumentLines = Guard.Against.NullOrEmpty(documentLines, nameof(DocumentLines), "Document Lines cannot be null or empty");
        Comments = "Posted from WMS";
        U_SchlYear = schlYear;
        U_DRNo = drNo;
        U_DelivMeans = delivMeans;
        U_Courier = courier;
        U_CourName = courName;
        U_ActualDelivDate = actualDelivDate;
        U_Desig = desig;
        U_WBNo = wbNo;
        U_PlateNo = plateNo;
        U_Driver = driver;
        U_Remarks = docRemarks;
        U_RecBy = recBy;
        U_AppBy = appBy;
        U_NotedBy = notedBy;
    }
}
