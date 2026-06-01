using Application.DataTransferObjects.Transactions.InventoryCounting;
using Domain.Entities.Enums.Transaction.InventoryCounting;
using Shared.Entities;

namespace Application.UseCases.Repositories.Domain.Transaction.InventoryCounting;

public interface IInventoryCountingReadRepo
{
    Task<(IEnumerable<InventoryCountingDataGridDTO> data, int count)> GetInventoryCountingDataGrid(DataGridIntent intent);
    Task<InventoryCountingDocumentDTO?> GetInventoryCountingDocument(Guid id);
    Task<bool> ExistsDocumentForWarehouseAndCycleInPeriodAsync(string whsCode, CycleType cycleType, DateTime countingDate);
}
