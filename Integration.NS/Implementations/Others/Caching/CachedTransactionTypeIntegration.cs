using Application.DataTransferObjects.Others.SAP;
using Application.UseCases.Repositories.Integration.Others;
using Integration.NS.Implementations.Vestigial;
using Microsoft.Extensions.Caching.Memory;

namespace Integration.NS.Implementations.Others.Caching;

/// <summary>
/// Per-process cache decorator for transaction-type reference reads (Req 1.1,
/// 1.2: 15-minute TTL). Transaction-type reads do not vary by caller.
/// </summary>
public class CachedTransactionTypeIntegration(
    TransactionTypeIntegration inner,
    IMemoryCache cache) : ITransactionTypeIntegration
{
    public const string InterfaceName = "TransactionType";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(15);

    public Task<IEnumerable<TransactionTypeSAPDTO>> GetTransactionTypesAsync()
    {
        var key = NsCacheKey.Build(InterfaceName, nameof(GetTransactionTypesAsync));
        return cache.GetOrAddAsync(key, Ttl, () => inner.GetTransactionTypesAsync());
    }
}
