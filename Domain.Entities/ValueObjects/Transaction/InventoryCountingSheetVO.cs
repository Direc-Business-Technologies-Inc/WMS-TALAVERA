using Ardalis.GuardClauses;
using Domain.Entities.Enums.Transaction.InventoryCounting;
using Domain.ValueObjects;
using Domain.ValueObjects.Transaction;

namespace Domain.Entities.ValueObjects.Transaction;

public class InventoryCountingSheetVO : ValueObject
{
    public AppDocNumVO SheetNo { get; private set; }
    public Guid InventoryCountingDocumentId { get; private set; }
    public Guid CounterId { get; private set; }
    public DateTime SubmittedDate { get; private set; }
    public InventoryCountingSheetStatus Status { get; private set; }

    public List<InventoryCountingSheetLineVO> SheetLines { get; private set; } = [];

    InventoryCountingSheetVO() { }

    public InventoryCountingSheetVO(AppDocNumVO sheetNo,
                                    Guid countingDocumentId,
                                    Guid counterId,
                                    DateTime submittedDate,
                                    List<InventoryCountingSheetLineVO> sheetLines)
    {
        SheetNo = Guard.Against.Null(sheetNo);
        InventoryCountingDocumentId = Guard.Against.NullOrEmpty(countingDocumentId);
        CounterId = Guard.Against.NullOrEmpty(counterId);
        SubmittedDate = Guard.Against.NullOrOutOfSQLDateRange(submittedDate);
        Status = InventoryCountingSheetStatus.Async;

        SheetLines = [.. sheetLines];
    }

    public InventoryCountingSheetVO SetStatus(InventoryCountingSheetStatus status)
    {
        Status = Guard.Against.EnumOutOfRange<InventoryCountingSheetStatus>(status, nameof(Status), "Inventory Counting Sheet Status must be a valid status state");
        return this;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return SheetNo;
        yield return InventoryCountingDocumentId;
        yield return CounterId;
        yield return SubmittedDate;
        yield return Status;
    }
}
