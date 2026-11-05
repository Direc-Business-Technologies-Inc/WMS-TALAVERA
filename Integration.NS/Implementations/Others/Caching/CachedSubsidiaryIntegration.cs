using Application.DataTransferObjects.Others;
using Application.UseCases.Repositories.Integration.Others;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Shared.Entities;

namespace Integration.NS.Implementations.Others.Caching;

/// <summary>
/// Per-process cache decorator for subsidiary reference reads. The list read is
/// scoped by the caller's <c>nsAllowedSubsidiaries</c> claim because the
/// concrete integration orders results by it.
/// </summary>
public class CachedSubsidiaryIntegration(
    SubsidiaryIntegration inner,
    IMemoryCache cache,
    IHttpContextAccessor accessor) : ISubsidiaryIntegration
{
    private const string InterfaceName = "Subsidiary";
    private const string AllowedSubsidiariesClaim = "com.direcbusiness.wms.nsAllowedSubsidiaries";
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(6);

    public Task<(IEnumerable<SubsidiaryDTO> Data, int Count)> GetSubsidiariesAsync(DataGridIntent intent, CancellationToken ct = default)
    {
        var scope = accessor.HttpContext?.User?.FindFirst(AllowedSubsidiariesClaim)?.Value;
        var key = NsCacheKey.Build(InterfaceName, nameof(GetSubsidiariesAsync), intent, scope);
        return cache.GetOrAddAsync(key, Ttl, () => inner.GetSubsidiariesAsync(intent, ct), ct);
    }

    public Task<(IEnumerable<SubsidiaryDTO> Data, int Count)> GetSubsidiariesByVendorAsync(DataGridIntent intent, int vendorId, CancellationToken ct = default)
    {
        var key = NsCacheKey.Build(InterfaceName, nameof(GetSubsidiariesByVendorAsync), intent, null, vendorId);
        return cache.GetOrAddAsync(key, Ttl, () => inner.GetSubsidiariesByVendorAsync(intent, vendorId, ct), ct);
    }

    public Task<(IEnumerable<SubsidiaryDTO> Data, int Count)> GetSubsidiariesByCustomerAsync(DataGridIntent intent, int customerId, CancellationToken ct = default)
    {
        var key = NsCacheKey.Build(InterfaceName, nameof(GetSubsidiariesByCustomerAsync), intent, null, customerId);
        return cache.GetOrAddAsync(key, Ttl, () => inner.GetSubsidiariesByCustomerAsync(intent, customerId, ct), ct);
    }

    public Task<IEnumerable<SubsidiaryDTO>> GetChildSubsidiariesAsync(int subsidiaryId, CancellationToken ct = default)
    {
        var key = NsCacheKey.Build(InterfaceName, nameof(GetChildSubsidiariesAsync), null, null, subsidiaryId);
        return cache.GetOrAddAsync(key, Ttl, () => inner.GetChildSubsidiariesAsync(subsidiaryId, ct), ct);
    }

    public Task<SubsidiaryDTO?> GetSubsidiaryAsync(int subsidiaryid, CancellationToken ct = default)
    {
        var key = NsCacheKey.Build(InterfaceName, nameof(GetSubsidiaryAsync), null, null, subsidiaryid);
        return cache.GetOrAddAsync(key, Ttl, () => inner.GetSubsidiaryAsync(subsidiaryid, ct), ct);
    }
}
