using Ardalis.GuardClauses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Integration.SAP.Entities.Transactional.InventoryTransfer;

public class InventoryTransferPayload
{
    public DateTime DocDate { get; private set; }
    public string FromWarehouse { get; private set; }
    public string ToWarehouse { get; private set; }
    public string Comments { get; private set; }
    public string? U_TransferType { get; private set; }
    public string? U_Remarks { get; private set; }
    public string U_PrepBy { get; private set; }
    public string? U_AppBy { get; private set; }
    public string? U_NotedBy { get; private set; }
    public string? U_SchlYear { get; private set; }
    public int DocObjectCode { get; private set; } = 67;
    public IEnumerable<InventoryTransferLinesPayload> StockTransferLines { get; private set; }

    protected InventoryTransferPayload(
        DateTime docDate,
        string fromWarehouse,
        string toWarehouse,
        string? transferType,
        string? remarks,
        string preparedBy,
        string? approvedBy,
        string? notedBy,
        string? schlYear,
        int? docEntry,
        int? docNum,
        IEnumerable<InventoryTransferLinesPayload> lines
    ) 
    { 
        DocDate = docDate;
        Comments = "Posted from WMS";
        FromWarehouse = Guard.Against.NullOrEmpty(fromWarehouse, nameof(FromWarehouse), "\"FromWarehouse\" cannot be empty");
        ToWarehouse = Guard.Against.NullOrEmpty(toWarehouse, nameof(ToWarehouse), "\"To Warehouse\" cannot be empty");
        U_TransferType = Guard.Against.NullOrEmpty(transferType, nameof(U_TransferType), "\"Transfer type\" cannot be empty");
        U_Remarks = remarks;
        U_PrepBy = Guard.Against.NullOrEmpty(preparedBy, nameof(U_PrepBy), "\"Prepared by\" cannot be empty");
        U_AppBy = approvedBy;  
        U_NotedBy = notedBy;
        U_SchlYear = schlYear;
        StockTransferLines = Guard.Against.NullOrEmpty(lines, nameof(StockTransferLines), "Document Lines cannot be null or empty");
    }

    public static InventoryTransferPayload Create(
        DateTime docDate,
        string fromWarehouse,
        string toWarehouse,
        string? transferType,
        string? remarks,
        string? schoolYear,
        string preparedBy,
        string? approvedBy,
        string? notedBy,
        int? docEntry,
        int? docNum,
        IEnumerable<InventoryTransferLinesPayload> lines
    )
    {
        return new InventoryTransferPayload(
            docDate: docDate,
            fromWarehouse: fromWarehouse,
            toWarehouse: toWarehouse,
            transferType: transferType,
            remarks: remarks,
            preparedBy: preparedBy,
            approvedBy: approvedBy,
            notedBy: notedBy,
            lines: lines,
            docEntry: docEntry, 
            docNum: docNum,
            schlYear: schoolYear
        );
    }
}
