using System.Security.Claims;
using Application.DataTransferObjects.Others;
using Integration.NS.Implementations.Others;
using Integration.NS.Implementations.Others.Caching;
using Integration.NS.Services;
using Integration.NS.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Shared.Entities;
using Xunit;

namespace Integration.NS.Tests.Implementations.Caching;

public class CachedReferenceIntegrationTests
{
    private const string EmployeeIdClaim = "com.direcbusiness.wms.nsEmployeeId";
    private const string AllowedSubsidiariesClaim = "com.direcbusiness.wms.nsAllowedSubsidiaries";

    private static HttpContextAccessor AccessorWithClaims(params (string Type, string Value)[] claims)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            claims.Select(c => new Claim(c.Type, c.Value))));
        return new HttpContextAccessor { HttpContext = context };
    }

    private static MemoryCache NewCache() => new(new MemoryCacheOptions());

    // AC6 — identical reference read twice => one NetSuite call.
    [Fact]
    public async Task Identical_location_read_is_cached()
    {
        var client = new CountingNetSuiteClient();
        var accessor = new HttpContextAccessor();
        var cached = new CachedLocationIntegration(
            new LocationIntegration(client, accessor, new SuiteQLQueryBuilderFactoryService()),
            NewCache(),
            accessor);

        var intent = new DataGridIntent { Take = 10 };

        await cached.GetLocationsAsync(intent);
        await cached.GetLocationsAsync(intent);

        Assert.Equal(1, client.ExecuteSuiteQlCalls);
    }

    [Fact]
    public async Task Different_intent_produces_a_separate_cache_entry()
    {
        var client = new CountingNetSuiteClient();
        var accessor = new HttpContextAccessor();
        var cached = new CachedLocationIntegration(
            new LocationIntegration(client, accessor, new SuiteQLQueryBuilderFactoryService()),
            NewCache(),
            accessor);

        await cached.GetLocationsAsync(new DataGridIntent { Take = 10 });
        await cached.GetLocationsAsync(new DataGridIntent { Take = 20 });

        Assert.Equal(2, client.ExecuteSuiteQlCalls);
    }

    // Per-user isolation for the explicit employeeId overload.
    [Fact]
    public async Task Allowed_locations_are_scoped_per_employee()
    {
        var client = new CountingNetSuiteClient();
        var accessor = new HttpContextAccessor();
        var cached = new CachedLocationIntegration(
            new LocationIntegration(client, accessor, new SuiteQLQueryBuilderFactoryService()),
            NewCache(),
            accessor);

        var intent = new DataGridIntent { Take = 10 };

        await cached.GetUserAllowedLocations(intent, 42);
        await cached.GetUserAllowedLocations(intent, 42);
        Assert.Equal(1, client.ExecuteSuiteQlCalls);

        await cached.GetUserAllowedLocations(intent, 43);
        Assert.Equal(2, client.ExecuteSuiteQlCalls);
    }

    [Fact]
    public async Task Current_user_allowed_locations_use_the_claim_as_scope()
    {
        var client = new CountingNetSuiteClient();
        var accessor = AccessorWithClaims((EmployeeIdClaim, "42"));
        var cached = new CachedLocationIntegration(
            new LocationIntegration(client, accessor, new SuiteQLQueryBuilderFactoryService()),
            NewCache(),
            accessor);

        var intent = new DataGridIntent { Take = 10 };

        await cached.GetCurrentUserAllowedLocations(intent);
        await cached.GetCurrentUserAllowedLocations(intent);

        Assert.Equal(1, client.ExecuteSuiteQlCalls);
    }

    // Claim absent => skip cache entirely (safe fallback, never cross-user).
    [Fact]
    public async Task Current_user_locations_without_claim_are_not_cached()
    {
        var client = new CountingNetSuiteClient();
        var accessor = new HttpContextAccessor();
        var cached = new CachedLocationIntegration(
            new LocationIntegration(client, accessor, new SuiteQLQueryBuilderFactoryService()),
            NewCache(),
            accessor);

        var first = await cached.GetCurrentUserAllowedLocations(new DataGridIntent { Take = 10 });
        var second = await cached.GetCurrentUserAllowedLocations(new DataGridIntent { Take = 10 });

        Assert.Empty(first.data);
        Assert.Empty(second.data);
        Assert.Equal(0, client.ExecuteSuiteQlCalls);
    }

    [Fact]
    public async Task Subsidiary_reads_are_scoped_by_the_allowed_subsidiaries_claim()
    {
        var client = new CountingNetSuiteClient();
        var cache = NewCache();
        var builder = new SuiteQLQueryBuilderFactoryService();
        var intent = new DataGridIntent { Take = 10 };

        var accessorA = AccessorWithClaims((AllowedSubsidiariesClaim, "[1,2]"));
        var cachedA = new CachedSubsidiaryIntegration(
            new SubsidiaryIntegration(client, accessorA, builder), cache, accessorA);

        await cachedA.GetSubsidiariesAsync(intent);
        await cachedA.GetSubsidiariesAsync(intent);
        Assert.Equal(1, client.ExecuteSuiteQlCalls);

        var accessorB = AccessorWithClaims((AllowedSubsidiariesClaim, "[3,4]"));
        var cachedB = new CachedSubsidiaryIntegration(
            new SubsidiaryIntegration(client, accessorB, builder), cache, accessorB);

        await cachedB.GetSubsidiariesAsync(intent);
        Assert.Equal(2, client.ExecuteSuiteQlCalls);
    }

    [Fact]
    public async Task Vendor_reads_are_cached()
    {
        var client = new CountingNetSuiteClient();
        var cached = new CachedVendorIntegration(
            new VendorIntegration(client, new SuiteQLQueryBuilderFactoryService()),
            NewCache());

        var intent = new DataGridIntent { Take = 10 };

        await cached.GetVendorsListAsync(intent);
        await cached.GetVendorsListAsync(intent);

        Assert.Equal(1, client.ExecuteSuiteQlCalls);
    }

    // Req 1.2 — department reads are cached per subsidiary scope.
    [Fact]
    public async Task Department_reads_are_cached()
    {
        var client = new CountingNetSuiteClient();
        var accessor = new HttpContextAccessor();
        var cached = new CachedDepartmentIntegration(
            new DepartmentIntegration(client, accessor, new SuiteQLQueryBuilderFactoryService()),
            NewCache(),
            accessor);

        var intent = new DataGridIntent { Take = 10 };

        await cached.GetDepartmentsList(intent);
        await cached.GetDepartmentsList(intent);

        Assert.Equal(1, client.ExecuteSuiteQlCalls);
    }

    [Fact]
    public async Task Department_reads_are_scoped_by_the_allowed_subsidiaries_claim()
    {
        var cache = NewCache();
        var builder = new SuiteQLQueryBuilderFactoryService();
        var intent = new DataGridIntent { Take = 10 };

        var client = new CountingNetSuiteClient();
        var accessorA = AccessorWithClaims((AllowedSubsidiariesClaim, "[1,2]"));
        var cachedA = new CachedDepartmentIntegration(
            new DepartmentIntegration(client, accessorA, builder), cache, accessorA);

        await cachedA.GetDepartmentsList(intent);
        await cachedA.GetDepartmentsList(intent);
        Assert.Equal(1, client.ExecuteSuiteQlCalls);

        var accessorB = AccessorWithClaims((AllowedSubsidiariesClaim, "[3,4]"));
        var cachedB = new CachedDepartmentIntegration(
            new DepartmentIntegration(client, accessorB, builder), cache, accessorB);

        await cachedB.GetDepartmentsList(intent);
        Assert.Equal(2, client.ExecuteSuiteQlCalls);
    }

    // Req 1.4 — N concurrent identical misses produce exactly 1 NetSuite call.
    [Fact]
    public async Task Concurrent_identical_misses_produce_a_single_call()
    {
        var client = new CountingNetSuiteClient();
        var accessor = new HttpContextAccessor();
        var cached = new CachedDepartmentIntegration(
            new DepartmentIntegration(client, accessor, new SuiteQLQueryBuilderFactoryService()),
            NewCache(),
            accessor);

        var intent = new DataGridIntent { Take = 10 };

        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => cached.GetDepartmentsList(intent)));

        Assert.Equal(1, client.ExecuteSuiteQlCalls);
    }

    // Factory failures are never cached — the next request retries NetSuite.
    [Fact]
    public async Task Failed_reads_are_not_cached()
    {
        var cache = NewCache();
        var calls = 0;

        async Task<int> FailingFactory()
        {
            calls++;
            await Task.Yield();
            throw new InvalidOperationException("NetSuite is down");
        }

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => cache.GetOrAddAsync("ns:cache:v1:Test:Method:abc", TimeSpan.FromMinutes(15), FailingFactory));

        var result = await cache.GetOrAddAsync(
            "ns:cache:v1:Test:Method:abc", TimeSpan.FromMinutes(15), () => Task.FromResult(42));

        Assert.Equal(42, result);
        Assert.Equal(1, calls);
    }

    // Req 1.3 — invalidation evicts so the next read hits NetSuite again.
    [Fact]
    public async Task Invalidation_evicts_reference_data_entries()
    {
        var client = new CountingNetSuiteClient();
        var accessor = new HttpContextAccessor();
        var cache = NewCache();
        var cached = new CachedDepartmentIntegration(
            new DepartmentIntegration(client, accessor, new SuiteQLQueryBuilderFactoryService()),
            cache,
            accessor);
        var invalidator = new ReferenceDataCacheInvalidator(cache);

        var intent = new DataGridIntent { Take = 10 };

        await cached.GetDepartmentsList(intent);
        await cached.GetDepartmentsList(intent);
        Assert.Equal(1, client.ExecuteSuiteQlCalls);

        invalidator.Invalidate(CachedDepartmentIntegration.InterfaceName);

        await cached.GetDepartmentsList(intent);
        Assert.Equal(2, client.ExecuteSuiteQlCalls);
    }

    // AC7 — transactional reads are never decorated and always come fresh.
    [Fact]
    public async Task Transactional_reads_are_never_cached()
    {
        var client = new CountingNetSuiteClient();
        var inventory = new InventoryIntegration(client, new SuiteQLQueryBuilderFactoryService());

        var intent = new DataGridIntent { Take = 10 };

        await inventory.GetInventoryBalance(intent);
        await inventory.GetInventoryBalance(intent);

        Assert.Equal(2, client.ExecuteSuiteQlCalls);
    }
}
