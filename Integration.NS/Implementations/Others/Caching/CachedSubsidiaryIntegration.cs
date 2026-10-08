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

    public Task<(IEnumerable<SubsidiaryDTO> Data, int Count)> GetSubsidiariesAsync(DataGridIntent intent)
    {
        var scope = accessor.HttpContext?.User?.FindFirst(AllowedSubsidiariesClaim)?.Value;
        var key = NsCacheKey.Build(InterfaceName, nameof(GetSubsidiariesAsync), intent, scope);
        return cache.GetOrAddAsync(key, Ttl, () => inner.GetSubsidiariesAsync(intent));
    }

    public Task<(IEnumerable<SubsidiaryDTO> Data, int Count)> GetSubsidiariesByVendorAsync(DataGridIntent intent, int vendorId)
    {
        var key = NsCacheKey.Build(InterfaceName, nameof(GetSubsidiariesByVendorAsync), intent, null, vendorId);
        return cache.GetOrAddAsync(key, Ttl, () => inner.GetSubsidiariesByVendorAsync(intent, vendorId));
    }

    public Task<(IEnumerable<SubsidiaryDTO> Data, int Count)> GetSubsidiariesByCustomerAsync(DataGridIntent intent, int customerId)
    {
        var key = NsCacheKey.Build(InterfaceName, nameof(GetSubsidiariesByCustomerAsync), intent, null, customerId);
        return cache.GetOrAddAsync(key, Ttl, () => inner.GetSubsidiariesByCustomerAsync(intent, customerId));
    }

    public Task<IEnumerable<SubsidiaryDTO>> GetChildSubsidiariesAsync(int subsidiaryId)
    {
        var key = NsCacheKey.Build(InterfaceName, nameof(GetChildSubsidiariesAsync), null, null, subsidiaryId);
        return cache.GetOrAddAsync(key, Ttl, () => inner.GetChildSubsidiariesAsync(subsidiaryId));
    }

    public Task<SubsidiaryDTO?> GetSubsidiaryAsync(int subsidiaryid)
    {
        var key = NsCacheKey.Build(InterfaceName, nameof(GetSubsidiaryAsync), null, null, subsidiaryid);
        return cache.GetOrAddAsync(key, Ttl, () => inner.GetSubsidiaryAsync(subsidiaryid));
    }
}
