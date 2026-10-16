using Application.DataTransferObjects.Others;
using Application.UseCases.Repositories.Integration.Others;
using Microsoft.Extensions.Caching.Memory;
using Shared.Entities;

namespace Integration.NS.Implementations.Others.Caching;

/// <summary>
/// Per-process cache decorator for vendor reference reads. No per-user scope:
/// vendor reads do not vary by caller.
/// </summary>
public class CachedVendorIntegration(
    VendorIntegration inner,
    IMemoryCache cache) : IVendorIntegration
{
    private const string InterfaceName = "Vendor";
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(6);

    public Task<(IEnumerable<VendorDTO> Data, int Count)> GetVendorsListAsync(DataGridIntent intent, CancellationToken ct = default)
    {
        var key = NsCacheKey.Build(InterfaceName, nameof(GetVendorsListAsync), intent);
        return cache.GetOrAddAsync(key, Ttl, () => inner.GetVendorsListAsync(intent, ct), ct);
    }

    public Task<(IEnumerable<VendorDTO> Data, int Count)> GetVendorsBySubsidiaryListAsync(DataGridIntent intent, int subsidiary, CancellationToken ct = default)
    {
        var key = NsCacheKey.Build(InterfaceName, nameof(GetVendorsBySubsidiaryListAsync), intent, null, subsidiary);
        return cache.GetOrAddAsync(key, Ttl, () => inner.GetVendorsBySubsidiaryListAsync(intent, subsidiary, ct), ct);
    }

    public Task<(IEnumerable<VendorDTO> Data, int Count)> GetTradeVendorsListAsync(DataGridIntent intent, CancellationToken ct = default)
    {
        var key = NsCacheKey.Build(InterfaceName, nameof(GetTradeVendorsListAsync), intent);
        return cache.GetOrAddAsync(key, Ttl, () => inner.GetTradeVendorsListAsync(intent, ct), ct);
    }

    public Task<(IEnumerable<VendorDTO> Data, int Count)> GetTradeVendorsBySubsidiaryListAsync(DataGridIntent intent, int subsidiary, CancellationToken ct = default)
    {
        var key = NsCacheKey.Build(InterfaceName, nameof(GetTradeVendorsBySubsidiaryListAsync), intent, null, subsidiary);
        return cache.GetOrAddAsync(key, Ttl, () => inner.GetTradeVendorsBySubsidiaryListAsync(intent, subsidiary, ct), ct);
    }

    public Task<(IEnumerable<VendorDTO> Data, int Count)> GetNonTradeVendorsListAsync(DataGridIntent intent, CancellationToken ct = default)
    {
        var key = NsCacheKey.Build(InterfaceName, nameof(GetNonTradeVendorsListAsync), intent);
        return cache.GetOrAddAsync(key, Ttl, () => inner.GetNonTradeVendorsListAsync(intent, ct), ct);
    }

    public Task<(IEnumerable<VendorDTO> Data, int Count)> GetNonTradeVendorsBySubsidiaryListAsync(DataGridIntent intent, int subsidiary, CancellationToken ct = default)
    {
        var key = NsCacheKey.Build(InterfaceName, nameof(GetNonTradeVendorsBySubsidiaryListAsync), intent, null, subsidiary);
        return cache.GetOrAddAsync(key, Ttl, () => inner.GetNonTradeVendorsBySubsidiaryListAsync(intent, subsidiary, ct), ct);
    }
}
