using Shared.Entities;
using Web.BlazorServer.ViewModels.Transaction.InventoryCounting;

namespace Web.BlazorServer.Handlers.Repositories.Transaction.InventoryCounting;

public interface IInventoryCountingHandler
{
    Task<(IEnumerable<InventoryCountingDataGridVM> Data, int Count)> GetInventoryCountingDataGridAsync(DataGridIntent intent);
    Task<InventoryCountingVM?> GetInventoryCountingDocumentAsync(Guid id);
    Task<bool> CreateInventoryCountingDocumentAsync(InventoryCountingVM data);
    Task<bool> SaveInventoryCountingDocumentAsync(Guid id);
    Task<bool> PostInventoryCountingDocumentAsync(Guid id);
    Task<bool> RecountInventoryCountingDocumentAsync(Guid id);
    Task<bool> CreateInventoryCountingSheetAsync(InventoryCountingSheetVM sheet);
    Task<bool> IgnoreInventoryCountingSheetAsync(Guid documentId, string sheetNo);
    Task<bool> SyncInventoryCountingSheetAsync(Guid documentId, string sheetNo);
    Task<IEnumerable<InventoryCountingLineVM>> GetWarehouseItemsForCountingAsync(string whsCode);
}
