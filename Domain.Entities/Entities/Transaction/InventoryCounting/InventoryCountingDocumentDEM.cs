using Ardalis.GuardClauses;
using Domain.Entities.Enums.Transaction.InventoryCounting;
using Domain.Entities.Transaction.Common;
using Domain.Entities.ValueObjects.Others;
using Domain.Entities.ValueObjects.Transaction;
using Domain.ValueObjects.Transaction;

namespace Domain.Entities.Entities.Transaction.InventoryCounting;

public class InventoryCountingDocumentDEM : TransactionalDocumentDEM
{
    public WarehouseVO Warehouse { get; private set; }
    public DateTime CountingDate { get; private set; }
    public CycleType CycleType { get; private set; }
    public InventoryCountingDocumentStatus Status { get; private set; }
    public string? Remarks { get; private set; }

    public List<InventoryCountingDocumentLineVO> _documentLines = [];
    public IReadOnlyCollection<InventoryCountingDocumentLineVO> DocumentLines => _documentLines.AsReadOnly();
    
    public List<InventoryCountingSheetVO> _sheets = [];
    public IReadOnlyCollection<InventoryCountingSheetVO> Sheets => _sheets.AsReadOnly();

    public InventoryCountingDocumentDEM() { }

    public InventoryCountingDocumentDEM(
        Guid documentTypeId,
        AppDocNumVO lsmsDocNum,
        WarehouseVO warehouse,
        DateTime countingDate,
        CycleType cycleType,
        List<InventoryCountingDocumentLineVO> documentLines,
        string? remarks = null,
        SapDocumentReferenceVO? sapReference = null) 
        : base(documentTypeId, lsmsDocNum, sapReference)
    {
        Warehouse = Guard.Against.Null(warehouse, nameof(WarehouseVO), "Warehouse Code cannot be null or empty");
        CountingDate = Guard.Against.NullOrOutOfSQLDateRange(countingDate, nameof(CountingDate), "Counting Date cannot be null or out of range");
        CycleType = Guard.Against.EnumOutOfRange<CycleType>(cycleType,  nameof(CycleType), "Cycle Type must be a valid Cycle");
        Status = InventoryCountingDocumentStatus.Open;
        Remarks = remarks;

        foreach (var line in documentLines)
            _documentLines.Add(line);
    }

    public InventoryCountingDocumentDEM UpdateStatus(InventoryCountingDocumentStatus status)
    {
        Status = Guard.Against.EnumOutOfRange<InventoryCountingDocumentStatus>(status, nameof(Status),  "Status must be a valid status type");
        return this;
    }

    public InventoryCountingDocumentDEM UpdateRemarks(string? remarks)
    {
        Remarks = remarks;
        return this;
    }

    public InventoryCountingDocumentDEM AddLine(InventoryCountingDocumentLineVO line)
    {
        _documentLines.Add(line);
        return this;
    }

    public InventoryCountingDocumentDEM AddSheet(InventoryCountingSheetVO sheet)
    {
        foreach (var sheetLine in sheet.SheetLines)
        {
            var docLine = _documentLines.FirstOrDefault(dl =>
                dl.ItemCode == sheetLine.ItemCode && dl.UoMCode == sheetLine.UoMCode);

            if (docLine is not null)
                docLine.UpdateActualQuantity(docLine.ActualQuantity + sheetLine.Quantity);

            sheetLine.SetStatus(InventoryCountingSheetStatus.Synced);
        }

        sheet.SetStatus(InventoryCountingSheetStatus.Synced);
        _sheets.Add(sheet);
        return this;
    }
}



