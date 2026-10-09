using Application.DataTransferObjects.Others.SAP;
using Application.UseCases.Repositories.Integration.Others;
using Integration.NS.Implementations.Vestigial;
using Microsoft.Extensions.Caching.Memory;
using Shared.Entities;

namespace Integration.NS.Implementations.Others.Caching;

/// <summary>
/// Per-process cache decorator for school-year reference reads (Req 1.1, 1.2:
/// 15-minute TTL). School-year reads do not vary by caller.
/// </summary>
public class CachedSchoolYearIntegration(
    SchoolYearIntegration inner,
    IMemoryCache cache) : ISchoolYearIntegration
{
    public const string InterfaceName = "SchoolYear";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(15);

    public Task<(IEnumerable<SchoolYearSAPDTO> Data, int Count)> GetAllSchoolYear(DataGridIntent intent)
    {
        var key = NsCacheKey.Build(InterfaceName, nameof(GetAllSchoolYear), intent);
        return cache.GetOrAddAsync(key, Ttl, () => inner.GetAllSchoolYear(intent));
    }
}
