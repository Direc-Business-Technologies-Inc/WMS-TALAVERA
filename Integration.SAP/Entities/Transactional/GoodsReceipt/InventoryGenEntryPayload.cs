using Ardalis.GuardClauses;

namespace Integration.SAP.Entities.Transactional.GoodsReceipt;

public class InventoryGenEntryPayload
{
    public string U_PrepBy { get; private set; }
    public string Comments { get; private set; }
    public string U_TransType { get; private set; }
    public string? U_BpCode { get; private set; }
    public string? U_BPName { get; private set; }
    public string? U_PURNo { get; private set; }
    public string? U_WARNo { get; private set; }
    public string? U_Desig { get; set; }
    public string? U_Remarks { get; set; }
    public string? U_AppBy { get; set; }
    public string? U_RecBy { get; set; }
    public string? U_NotedBy { get; set; }
    public IEnumerable<InventoryGenEntryLinesPayload> DocumentLines { get; private set; } = [];

    public InventoryGenEntryPayload(string preparedBy,
                                    string transType,
                                    IEnumerable<InventoryGenEntryLinesPayload> lines,
                                    string? bpCode = null,
                                    string? bpName = null,
                                    string? purNo = null,
                                    string? warNo = null,
                                    string? designation = null,
                                    string? recievedBy = null,
                                    string? remarks = null,
                                    string? appBy = null,
                                    string? notedBy = null)
    {
        U_PrepBy = Guard.Against.NullOrEmpty(preparedBy, nameof(U_PrepBy), "Prepared By cannot be null or empty");
        U_TransType = Guard.Against.NullOrEmpty(transType, nameof(U_TransType), "Transaction Type cannot be null or empty");
        DocumentLines = Guard.Against.NullOrEmpty(lines, nameof(DocumentLines), "Document Lines cannot be null or empty");
        Comments = "Posted from WMS";
        U_BpCode = bpCode;
        U_BPName = bpName;
        U_PURNo = purNo;
        U_WARNo = warNo;
        U_Desig = designation;
        U_RecBy = recievedBy;
        U_Remarks = remarks;
        U_AppBy = appBy;
        U_NotedBy = notedBy;
    }
}
