using Domain.Entities.Enums.Transaction.InventoryCounting;

namespace Web.BlazorServer.ViewModels.Transaction.InventoryCounting;

public class InventoryCountingDataGridVM
{
    public Guid Id { get; set; }
    public string AppDocNum { get; set; } = string.Empty;
    public string Warehouse { get; set; } = string.Empty;
    public CycleType CycleType { get; set; }
    public InventoryCountingDocumentStatus Status { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? Remarks { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}
