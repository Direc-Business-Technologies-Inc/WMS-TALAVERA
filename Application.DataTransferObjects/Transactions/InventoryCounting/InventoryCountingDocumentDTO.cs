using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Transactions.Commons;
using Domain.Entities.Enums.Transaction.InventoryCounting;

namespace Application.DataTransferObjects.Transactions.InventoryCounting;

public class InventoryCountingDocumentDTO : TransactionalDocumentDTO
{
    public WarehouseDTO Warehouse { get; set; } = new();
    public DateTime CountingDate { get; set; }
    public CycleType CycleType { get; set; }
    public InventoryCountingDocumentStatus Status { get; set; }
    public string? Remarks { get; set; } = null;

    public IEnumerable<InventoryCountingDocumentLineDTO> DocumentLines { get; set; } = [];
    public IEnumerable<InventoryCountingSheetDTO> Sheets { get; set; } = [];
}
