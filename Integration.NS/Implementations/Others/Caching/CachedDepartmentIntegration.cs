using Application.DataTransferObjects.Others;
using Application.UseCases.Repositories.Integration.Others;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Shared.Entities;

namespace Integration.NS.Implementations.Others.Caching;

/// <summary>
/// Per-process cache decorator for department reference reads (Req 1.1, 1.2:
/// 15-minute TTL). The read is scoped by the caller's
/// <c>nsAllowedSubsidiaries</c> claim because the concrete integration filters
/// by it — same approach as <see cref="CachedSubsidiaryIntegration"/>.
/// </summary>
public class CachedDepartmentIntegration(
    DepartmentIntegration inner,
    IMemoryCache cache,
    IHttpContextAccessor accessor) : IDepartmentIntegration
{
    public const string InterfaceName = "Department";
    private const string AllowedSubsidiariesClaim = "com.direcbusiness.wms.nsAllowedSubsidiaries";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(15);

    public Task<(IEnumerable<DepartmentDTO>, int)> GetDepartmentsList(DataGridIntent intent, CancellationToken ct = default)
    {
        var scope = accessor.HttpContext?.User?.FindFirst(AllowedSubsidiariesClaim)?.Value;
        var key = NsCacheKey.Build(InterfaceName, nameof(GetDepartmentsList), intent, scope);
        return cache.GetOrAddAsync(key, Ttl, () => inner.GetDepartmentsList(intent, ct), ct);
    }
}
