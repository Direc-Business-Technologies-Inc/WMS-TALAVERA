using Application.DataTransferObjects.Others.SAP;
using Application.UseCases.Repositories.Integration.Others;
using Microsoft.Extensions.Caching.Memory;
using Shared.Entities;

namespace Integration.NS.Implementations.Others.Caching;

/// <summary>
/// Per-process cache decorator for warehouse reference reads (Req 1.1, 1.2:
/// 15-minute TTL). Warehouse reads do not vary by caller.
/// </summary>
public class CachedWarehouseMasterDataIntegration(
    WarehouseMasterDataIntegration inner,
    IMemoryCache cache) : IWarehouseMasterDataIntegration
{
    public const string InterfaceName = "WarehouseMasterData";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(15);

    public Task<(IEnumerable<WarehouseSelectionSAPDTO> Data, int Count)> GetWarehouseAsync(DataGridIntent intent, CancellationToken ct = default)
    {
        var key = NsCacheKey.Build(InterfaceName, nameof(GetWarehouseAsync), intent);
        return cache.GetOrAddAsync(key, Ttl, () => inner.GetWarehouseAsync(intent, ct), ct);
    }
}
