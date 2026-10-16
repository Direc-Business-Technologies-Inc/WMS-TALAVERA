using Application.DataTransferObjects.Others;
using Application.UseCases.Repositories.Integration.Others;
using Shared.Entities;

namespace Integration.NS.Implementations.Others.Caching;

/// <summary>
/// 60-second grid cache for the item picker grids (Req 2.1). Items are
/// read-only from the web layer (no mutation methods on the integration), so
/// only the paginated grid reads are cached and there is nothing to evict.
/// </summary>
public class CachedItemsIntegration(
    ItemsIntegration inner,
    GridResultCache cache) : IItemsIntegration
{
    public const string EntityName = "Items";

    public Task<(IEnumerable<ItemsDTO> Data, int Count)> GetItemsDataGridAsync(DataGridIntent intent, CancellationToken ct = default)
        => cache.GetOrAddAsync(EntityName, nameof(GetItemsDataGridAsync), intent,
            () => inner.GetItemsDataGridAsync(intent, ct), ct);

    public Task<(IEnumerable<ItemsDTO> Data, int Count)> GetItemsByLocationDataGridAsync(DataGridIntent intent, int location, int? subsidiary = null, CancellationToken ct = default)
        => cache.GetOrAddAsync(EntityName, nameof(GetItemsByLocationDataGridAsync), intent,
            () => inner.GetItemsByLocationDataGridAsync(intent, location, subsidiary, ct), ct, location, subsidiary);

    public Task<(IEnumerable<ItemUnitDTO> Data, int Count)> GetItemUnits(ItemsDTO itemId, DataGridIntent intent, CancellationToken ct = default)
        => inner.GetItemUnits(itemId, intent, ct);

    public Task<(IEnumerable<ItemUnitDTO> Data, int Count)> GetItemUnits(int itemId, DataGridIntent intent, CancellationToken ct = default)
        => inner.GetItemUnits(itemId, intent, ct);

    public Task<ItemsDTO?> GetItem(string id, CancellationToken ct = default)
        => inner.GetItem(id, ct);
}