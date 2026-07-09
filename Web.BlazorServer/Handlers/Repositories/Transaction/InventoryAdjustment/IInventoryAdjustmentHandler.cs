using Shared.Entities;
using Web.BlazorServer.ViewModels.Transaction.InventoryAdjustment;

namespace Web.BlazorServer.Handlers.Repositories.Transaction.InventoryAdjustment;

public interface IInventoryAdjustmentHandler
{
    Task<(IEnumerable<InventoryAdjustmentDataGridVM> Data, int Count)> GetInventoryAdjustmentsDataGridAsync(DataGridIntent intent);
    Task<InventoryAdjustmentVM?> GetInventoryAdjustmentAsync(string id);
    Task<bool> CreateInventoryAdjustmentAsync(InventoryAdjustmentVM vm);
    Task<(IEnumerable<InventoryAdjustmentReasonVM> Data, int Count)> GetInventoryAdjustmentReasonsAsync(DataGridIntent intent);
    Task<(IEnumerable<InventoryAdjustmentCategoryVM> Data, int Count)> GetInventoryAdjustmentCategoriesAsync(DataGridIntent intent);
    Task<(IEnumerable<InventoryAdjustmentDataGridVM> Data, int Count)> GetIssuesDataGridAsync(DataGridIntent intent);
    Task<(IEnumerable<InventoryAdjustmentDataGridVM> Data, int Count)> GetReceiptsDataGridAsync(DataGridIntent intent);
}
