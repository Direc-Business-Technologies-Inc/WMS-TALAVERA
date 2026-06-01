using Application.DataTransferObjects.Administration.User;
using Application.DataTransferObjects.Transactions.Commons;
using Domain.Entities.Enums.Transaction.InventoryCounting;

namespace Application.DataTransferObjects.Transactions.InventoryCounting;

public class InventoryCountingSheetDTO
{
    public AppDocNumDTO SheetNo { get; set; } = new();
    public Guid InventoryCountingDocumentId { get; set; }
    public UserDetailDTO Counter { get; set; } = new();
    public DateTime SubmittedDate { get; set; }
    public InventoryCountingSheetStatus Status { get; set; }

    public IEnumerable<InventoryCountingSheetLineDTO> SheetLines { get; set; } = [];
}
