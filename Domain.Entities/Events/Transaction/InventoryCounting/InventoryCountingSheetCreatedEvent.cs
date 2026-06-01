using Domain.Events.Bases;

namespace Domain.Entities.Events.Transaction.InventoryCounting;

public class InventoryCountingSheetCreatedEvent(Guid documentId, string sheetNo) : DomainBaseEvent
{
    public Guid DocumentId { get; init; } = documentId;
    public string SheetNo { get; init; } = sheetNo;
}
