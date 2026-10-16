using Application.DataTransferObjects.Transactions.InventoryAdjustment;
using Application.UseCases.Repositories.Integration.Transaction.InventoryAdjustment;
using Shared.Entities;

namespace Integration.NS.Implementations.Others.Caching;

/// <summary>
/// 60-second grid cache for inventory adjustments (Req 2.1). Only the paginated
/// grid reads are cached; document reads and mutations pass straight through,
/// and a successful mutation evicts the entity's cached grid pages (Req 2.2).
/// </summary>
public class CachedInventoryAdjustmentIntegration(
    Transactions.InventoryAdjustmentIntegration inner,
    GridResultCache cache) : IInventoryAdjustmentIntegration
{
    public const string EntityName = "InventoryAdjustment";

    public Task<(IEnumerable<InventoryAdjustmentDataGridDTO> Data, int Count)> GetInventoryAdjustmentsAsync(DataGridIntent intent)
        => cache.GetOrAddAsync(EntityName, nameof(GetInventoryAdjustmentsAsync), intent,
            () => inner.GetInventoryAdjustmentsAsync(intent));

    public Task<(IEnumerable<InventoryAdjustmentDataGridDTO> Data, int Count)> GetIssuesAdjustmentsAsync(DataGridIntent intent)
        => cache.GetOrAddAsync(EntityName, nameof(GetIssuesAdjustmentsAsync), intent,
            () => inner.GetIssuesAdjustmentsAsync(intent));

    public Task<(IEnumerable<InventoryAdjustmentDataGridDTO> Data, int Count)> GetReceiptsAdjustmentsAsync(DataGridIntent intent)
        => cache.GetOrAddAsync(EntityName, nameof(GetReceiptsAdjustmentsAsync), intent,
            () => inner.GetReceiptsAdjustmentsAsync(intent));

    public Task<InventoryAdjustmentDTO?> GetInventoryAdjustmentAsync(string id)
        => inner.GetInventoryAdjustmentAsync(id);

    public Task<IEnumerable<InventoryAdjustmentLineDTO>> GetInventoryAdjustmentLinesAsync(string id)
        => inner.GetInventoryAdjustmentLinesAsync(id);

    public Task<(IEnumerable<InventoryAdjustmentReasonDTO> Data, int Count)> GetInventoryAdjustmentReasonsAsync(DataGridIntent intent)
        => inner.GetInventoryAdjustmentReasonsAsync(intent);

    public Task<(IEnumerable<InventoryAdjustmentCategoryDTO> Data, int Count)> GetInventoryAdjustmentCategoriesAsync(DataGridIntent intent)
        => inner.GetInventoryAdjustmentCategoriesAsync(intent);

    public async Task<bool> CreateInventoryAdjustment(InventoryAdjustmentDTO value)
    {
        var created = await inner.CreateInventoryAdjustment(value);
        if (created)
        {
            cache.Invalidate(EntityName);
        }

        return created;
    }
}