using Domain.Entities.Enums.Transaction.InventoryCounting;
using Web.BlazorServer.ViewModels.Others;
using Web.BlazorServer.ViewModels.Transaction.Commons;

namespace Web.BlazorServer.ViewModels.Transaction.InventoryCounting;

public class InventoryCountingVM : TransactionalDocumentVM
{
    public WarehouseVM Warehouse { get; set; } = new();
    public DateTime CountingDate { get; set; } = DateTime.Today;
    public CycleType CycleType { get; set; }
    public InventoryCountingDocumentStatus Status { get; set; }
    public string? Remarks { get; set; } = null;
    public IEnumerable<InventoryCountingLineVM> DocumentLines { get; set; } = [];
    public IEnumerable<InventoryCountingSheetVM> Sheets { get; set; } = [];
}
