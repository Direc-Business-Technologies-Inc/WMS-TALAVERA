using Application.DataTransferObjects.Transactions.InventoryTransfer.SAP;
using Application.UseCases.Repositories.Integration.Others;
using Integration.NS.Implementations.Vestigial;
using Microsoft.Extensions.Caching.Memory;

namespace Integration.NS.Implementations.Others.Caching;

/// <summary>
/// Per-process cache decorator for transfer-type reference reads (Req 1.1,
/// 1.2: 15-minute TTL). Transfer-type reads do not vary by caller.
/// </summary>
public class CachedTransferTypeIntegration(
    TransferTypeIntegration inner,
    IMemoryCache cache) : ITransferTypeIntegration
{
    public const string InterfaceName = "TransferType";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(15);

    public Task<IEnumerable<TransferTypeSAPDTO>> GetTransferTypesAsync()
    {
        var key = NsCacheKey.Build(InterfaceName, nameof(GetTransferTypesAsync));
        return cache.GetOrAddAsync(key, Ttl, () => inner.GetTransferTypesAsync());
    }
}
