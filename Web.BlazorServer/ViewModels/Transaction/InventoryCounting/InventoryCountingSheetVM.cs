using Domain.Entities.Enums.Transaction.InventoryCounting;
using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.Transaction.Commons;

namespace Web.BlazorServer.ViewModels.Transaction.InventoryCounting;

public class InventoryCountingSheetVM
{
    public AppDocNumVM SheetNo { get; set; } = new();
    public Guid InventoryCountingDocumentId { get; set; }
    public UserDetailVM Counter { get; set; } = new();
    public DateTime SubmittedDate { get; set; }
    public InventoryCountingSheetStatus Status { get; set; }
    public IEnumerable<InventoryCountingSheetLineVM> SheetLines { get; set; } = [];
}
