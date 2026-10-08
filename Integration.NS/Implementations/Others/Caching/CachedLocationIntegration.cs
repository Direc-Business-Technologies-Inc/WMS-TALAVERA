using Application.DataTransferObjects.Others;
using Application.UseCases.Repositories.Integration.Others;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Shared.Entities;

namespace Integration.NS.Implementations.Others.Caching;

/// <summary>
/// Per-process cache decorator for location reference reads. Wraps the concrete
/// <see cref="LocationIntegration"/> without modifying it; flush is process
/// restart (no cross-process invalidation in this phase).
/// </summary>
public class CachedLocationIntegration(
    LocationIntegration inner,
    IMemoryCache cache,
    IHttpContextAccessor accessor) : ILocationIntegration
{
    private const string InterfaceName = "Location";
    private const string EmployeeIdClaim = "com.direcbusiness.wms.nsEmployeeId";
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(6);

    public Task<(IEnumerable<LocationDTO> data, int count)> GetLocationsAsync(DataGridIntent intent)
    {
        var key = NsCacheKey.Build(InterfaceName, nameof(GetLocationsAsync), intent);
        return cache.GetOrAddAsync(key, Ttl, () => inner.GetLocationsAsync(intent));
    }

    public Task<(IEnumerable<LocationDTO> data, int count)> GetLocationsBySubsidiaryAsync(DataGridIntent intent, int subsidiaryId)
    {
        var key = NsCacheKey.Build(InterfaceName, nameof(GetLocationsBySubsidiaryAsync), intent, null, subsidiaryId);
        return cache.GetOrAddAsync(key, Ttl, () => inner.GetLocationsBySubsidiaryAsync(intent, subsidiaryId));
    }

    public Task<(IEnumerable<LocationDTO> data, int count)> GetSublocationsOfLocationAsync(DataGridIntent intent, int location)
    {
        var key = NsCacheKey.Build(InterfaceName, nameof(GetSublocationsOfLocationAsync), intent, null, location);
        return cache.GetOrAddAsync(key, Ttl, () => inner.GetSublocationsOfLocationAsync(intent, location));
    }

    public Task<(IEnumerable<LocationDTO> data, int count)> GetCurrentUserAllowedLocations(DataGridIntent intent)
    {
        // Per-user data is cached only when the employee scope is known; without
        // the claim we deliberately skip caching to avoid cross-user leakage.
        var claimValue = accessor.HttpContext?.User?.FindFirst(EmployeeIdClaim)?.Value;
        if (!int.TryParse(claimValue, out var employeeId))
        {
            return inner.GetCurrentUserAllowedLocations(intent);
        }

        return GetUserAllowedLocations(intent, employeeId);
    }

    public Task<(IEnumerable<LocationDTO> data, int count)> GetUserAllowedLocations(DataGridIntent intent, int employeeId)
    {
        var key = NsCacheKey.Build(
            InterfaceName,
            nameof(GetUserAllowedLocations),
            intent,
            employeeId.ToString());
        return cache.GetOrAddAsync(key, Ttl, () => inner.GetUserAllowedLocations(intent, employeeId));
    }

    public Task<LocationDTO?> GetLocation(int locationId)
    {
        var key = NsCacheKey.Build(InterfaceName, nameof(GetLocation), null, null, locationId);
        return cache.GetOrAddAsync(key, Ttl, () => inner.GetLocation(locationId));
    }

    public Task<LocationDTO?> GetParentLocation(int locationId)
    {
        var key = NsCacheKey.Build(InterfaceName, nameof(GetParentLocation), null, null, locationId);
        return cache.GetOrAddAsync(key, Ttl, () => inner.GetParentLocation(locationId));
    }

    public Task<(IEnumerable<LocationBinDTO> data, int count)> GetLocationBinsAsync(int locationId, DataGridIntent intent)
    {
        var key = NsCacheKey.Build(InterfaceName, nameof(GetLocationBinsAsync), intent, null, locationId);
        return cache.GetOrAddAsync(key, Ttl, () => inner.GetLocationBinsAsync(locationId, intent));
    }
}
