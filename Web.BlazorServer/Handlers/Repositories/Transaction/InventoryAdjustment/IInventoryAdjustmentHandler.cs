using Shared.Entities;
using Web.BlazorServer.ViewModels.Transaction.InventoryAdjustment;

namespace Web.BlazorServer.Handlers.Repositories.Transaction.InventoryAdjustment;

public interface IInventoryAdjustmentHandler
{
    Task<(IEnumerable<InventoryAdjustmentDataGridVM> Data, int Count)> GetInventoryAdjustmentsDataGridAsync(DataGridIntent intent);
    Task<InventoryAdjustmentVM?> GetInventoryAdjustmentAsync(string id);
    Task<bool> CreateInventoryAdjustmentAsync(InventoryAdjustmentVM vm);
    Task<(IEnumerable<InventoryAdjustmentReasonVM> Data, int Count)> GetInventoryAdjustmentReasonsAsync(DataGridIntent intent);
}
