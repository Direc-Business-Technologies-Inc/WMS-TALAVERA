using Integration.NS.Implementations.Others.Caching;
using Integration.NS.Tests.Fakes;
using Xunit;

namespace Integration.NS.Tests.Implementations.Caching;

/// <summary>
/// Req 2 — grid result caching: short TTL, per-user scoping, mutation-driven
/// eviction, and size-capped LRU eviction.
/// </summary>
public class GridResultCacheTests
{
    private static GridResultCache NewCache(Guid? userId = null) =>
        new(
            new GridCacheStore(100, TimeSpan.FromSeconds(60)),
            null,
            new StubCurrentUserService(userId ?? Guid.NewGuid()));

    private static CachedInventoryAdjustmentIntegration NewAdjustmentDecorator(
        MutatingNetSuiteClient client, Guid? userId = null)
        => new(
            new Integration.NS.Implementations.Transactions.InventoryAdjustmentIntegration(
                client,
                new Microsoft.AspNetCore.Http.HttpContextAccessor(),
                new Integration.NS.Services.SuiteQLQueryBuilderFactoryService()),
            NewCache(userId));

    [Fact]
    public async Task Identical_grid_intent_is_served_from_cache()
    {
        var client = new MutatingNetSuiteClient();
        var cached = NewAdjustmentDecorator(client);
        var intent = new Shared.Entities.DataGridIntent { Take = 10 };

        var first = await cached.GetInventoryAdjustmentsAsync(intent);
        var second = await cached.GetInventoryAdjustmentsAsync(new Shared.Entities.DataGridIntent { Take = 10 });

        Assert.Equal(1, client.ExecuteSuiteQlCalls);
        Assert.Equal(first.Count, second.Count);
    }

    [Fact]
    public async Task Different_intent_produces_a_separate_cache_entry()
    {
        var client = new MutatingNetSuiteClient();
        var cached = NewAdjustmentDecorator(client);

        await cached.GetInventoryAdjustmentsAsync(new Shared.Entities.DataGridIntent { Take = 10 });
        await cached.GetInventoryAdjustmentsAsync(new Shared.Entities.DataGridIntent { Take = 20 });

        Assert.Equal(2, client.ExecuteSuiteQlCalls);
    }

    [Fact]
    public async Task Different_grid_methods_do_not_share_entries()
    {
        var client = new MutatingNetSuiteClient();
        var cached = NewAdjustmentDecorator(client);
        var intent = new Shared.Entities.DataGridIntent { Take = 10 };

        await cached.GetInventoryAdjustmentsAsync(intent);
        await cached.GetIssuesAdjustmentsAsync(new Shared.Entities.DataGridIntent { Take = 10 });

        Assert.Equal(2, client.ExecuteSuiteQlCalls);
    }

    // Req 2.4 — two users with the same intent must not share an entry.
    [Fact]
    public async Task Grid_entries_are_scoped_per_user()
    {
        var client = new MutatingNetSuiteClient();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var intent = new Shared.Entities.DataGridIntent { Take = 10 };

        var cachedA = NewAdjustmentDecorator(client, userA);
        await cachedA.GetInventoryAdjustmentsAsync(intent);
        await cachedA.GetInventoryAdjustmentsAsync(new Shared.Entities.DataGridIntent { Take = 10 });
        Assert.Equal(1, client.ExecuteSuiteQlCalls);

        var cachedB = NewAdjustmentDecorator(client, userB);
        await cachedB.GetInventoryAdjustmentsAsync(intent);
        Assert.Equal(2, client.ExecuteSuiteQlCalls);
    }

    // Req 2.4 — an unidentified caller is never served from cache.
    [Fact]
    public async Task Unidentified_users_are_never_cached()
    {
        var client = new MutatingNetSuiteClient();
        var store = new GridCacheStore(100, TimeSpan.FromSeconds(60));
        var cache = new GridResultCache(store, null, new StubCurrentUserService(Guid.Empty));
        var cached = new CachedInventoryAdjustmentIntegration(
            new Integration.NS.Implementations.Transactions.InventoryAdjustmentIntegration(
                client,
                new Microsoft.AspNetCore.Http.HttpContextAccessor(),
                new Integration.NS.Services.SuiteQLQueryBuilderFactoryService()),
            cache);

        await cached.GetInventoryAdjustmentsAsync(new Shared.Entities.DataGridIntent { Take = 10 });
        await cached.GetInventoryAdjustmentsAsync(new Shared.Entities.DataGridIntent { Take = 10 });

        Assert.Equal(2, client.ExecuteSuiteQlCalls);
    }

    // Req 2.2 — a successful mutation evicts the entity's cached grid pages.
    [Fact]
    public async Task Successful_mutation_evicts_cached_grid_entries()
    {
        var client = new MutatingNetSuiteClient();
        var cached = NewAdjustmentDecorator(client);
        var intent = new Shared.Entities.DataGridIntent { Take = 10 };

        await cached.GetInventoryAdjustmentsAsync(intent);
        await cached.GetInventoryAdjustmentsAsync(new Shared.Entities.DataGridIntent { Take = 10 });
        Assert.Equal(1, client.ExecuteSuiteQlCalls);

        await cached.CreateInventoryAdjustment(new Application.DataTransferObjects.Transactions.InventoryAdjustment.InventoryAdjustmentDTO
        {
            Lines = new List<Application.DataTransferObjects.Transactions.InventoryAdjustment.InventoryAdjustmentLineDTO>(),
        });

        await cached.GetInventoryAdjustmentsAsync(intent);
        Assert.Equal(2, client.ExecuteSuiteQlCalls);
    }

    [Fact]
    public async Task Failed_mutation_keeps_cached_grid_entries()
    {
        var client = new MutatingNetSuiteClient { WriteSucceeds = false };
        var cached = NewAdjustmentDecorator(client);
        var intent = new Shared.Entities.DataGridIntent { Take = 10 };

        await cached.GetInventoryAdjustmentsAsync(intent);
        Assert.Equal(1, client.ExecuteSuiteQlCalls);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => cached.CreateInventoryAdjustment(new Application.DataTransferObjects.Transactions.InventoryAdjustment.InventoryAdjustmentDTO
            {
                Lines = new List<Application.DataTransferObjects.Transactions.InventoryAdjustment.InventoryAdjustmentLineDTO>(),
            }));

        await cached.GetInventoryAdjustmentsAsync(new Shared.Entities.DataGridIntent { Take = 10 });
        Assert.Equal(1, client.ExecuteSuiteQlCalls);
    }

    // Req 1.4-style single-flight carried over to grid misses.
    [Fact]
    public async Task Concurrent_identical_misses_produce_a_single_call()
    {
        var client = new MutatingNetSuiteClient();
        var cache = NewCache();
        var cached = new CachedInventoryAdjustmentIntegration(
            new Integration.NS.Implementations.Transactions.InventoryAdjustmentIntegration(
                client,
                new Microsoft.AspNetCore.Http.HttpContextAccessor(),
                new Integration.NS.Services.SuiteQLQueryBuilderFactoryService()),
            cache);

        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ =>
            cached.GetInventoryAdjustmentsAsync(new Shared.Entities.DataGridIntent { Take = 10 })));

        Assert.Equal(1, client.ExecuteSuiteQlCalls);
    }

    // Factory failures are never cached.
    [Fact]
    public async Task Failed_reads_are_not_cached()
    {
        var store = new GridCacheStore(100, TimeSpan.FromSeconds(60));
        var cache = new GridResultCache(store, null, new StubCurrentUserService(Guid.NewGuid()));
        var calls = 0;

        async Task<int> Failing() => throw new InvalidOperationException("NetSuite is down");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => cache.GetOrAddAsync("Test", "M", null, Failing));

        var second = await cache.GetOrAddAsync("Test", "M", null, () => { calls++; return Task.FromResult(7); });

        Assert.Equal(7, second);
        Assert.Equal(1, calls);
    }

    // Req 2.3 — the size cap evicts so a long session cannot grow unbounded.
    [Fact]
    public async Task Cache_capacity_is_capped()
    {
        var client = new MutatingNetSuiteClient();
        var store = new GridCacheStore(3, TimeSpan.FromSeconds(60));
        var cached = new CachedInventoryAdjustmentIntegration(
            new Integration.NS.Implementations.Transactions.InventoryAdjustmentIntegration(
                client,
                new Microsoft.AspNetCore.Http.HttpContextAccessor(),
                new Integration.NS.Services.SuiteQLQueryBuilderFactoryService()),
            new GridResultCache(store, null, new StubCurrentUserService(Guid.NewGuid())));

        for (var take = 1; take <= 10; take++)
        {
            await cached.GetInventoryAdjustmentsAsync(new Shared.Entities.DataGridIntent { Take = take });
        }

        // MemoryCache compacts asynchronously, so allow the sweep to run.
        for (var i = 0; i < 50 && store.Count > 3; i++)
        {
            await Task.Delay(20);
        }

        Assert.True(store.Count <= 3, $"expected at most 3 entries, found {store.Count}");
    }

    // Req 2.3 — the key index must track the cache, not outlive it. Before the
    // post-eviction callback, KnownKeys grew by one entry per distinct intent
    // while the cache itself stayed capped at 3.
    [Fact]
    public async Task Key_index_does_not_outlive_the_cache_it_tracks()
    {
        var client = new MutatingNetSuiteClient();
        var store = new GridCacheStore(3, TimeSpan.FromSeconds(60));
        var cache = new GridResultCache(store, null, new StubCurrentUserService(Guid.NewGuid()));
        var cached = new CachedInventoryAdjustmentIntegration(
            new Integration.NS.Implementations.Transactions.InventoryAdjustmentIntegration(
                client,
                new Microsoft.AspNetCore.Http.HttpContextAccessor(),
                new Integration.NS.Services.SuiteQLQueryBuilderFactoryService()),
            cache);

        for (var take = 1; take <= 25; take++)
        {
            await cached.GetInventoryAdjustmentsAsync(new Shared.Entities.DataGridIntent { Take = take });
        }

        // MemoryCache compaction and the eviction callbacks that follow it run
        // asynchronously, so wait for the index to converge rather than
        // sampling at a fixed instant: a parallel test run can delay the
        // compaction sweep well past a short timeout.
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (store.KnownKeys.Count > 3 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
        }

        Assert.True(
            store.KnownKeys.Count <= 3,
            $"expected the key index to converge within the cache cap, found {store.KnownKeys.Count}");
    }

    // Req 2.3 — single-flight gates are a fixed-size stripe array, not a
    // per-key dictionary, so they cannot grow with the number of distinct keys.
    [Fact]
    public void Gate_selection_is_bounded_regardless_of_key_count()
    {
        var store = new GridCacheStore(100, TimeSpan.FromSeconds(60));

        var gates = Enumerable
            .Range(0, 5_000)
            .Select(i => store.GateFor($"ns:grid:v1:Items:GetItemsAsync:user:{i}"))
            .ToList();

        // 5,000 distinct keys must resolve to a small fixed set of gates.
        Assert.True(
            gates.Distinct().Count() <= 64,
            $"expected at most 64 stripes, found {gates.Distinct().Count()}");
    }

    // Req 2.4 — a re-cached key must stay reachable by prefix invalidation.
    // The post-eviction callback ignores EvictionReason.Replaced precisely so a
    // background "Replaced" notification cannot strand a live entry.
    [Fact]
    public async Task Rewriting_a_key_keeps_it_invalidatable()
    {
        var client = new MutatingNetSuiteClient();
        var store = new GridCacheStore(100, TimeSpan.FromSeconds(60));
        var cache = new GridResultCache(store, null, new StubCurrentUserService(Guid.NewGuid()));

        await cache.GetOrAddAsync("Receiving", "Orders", new Shared.Entities.DataGridIntent { Take = 10 }, () => Task.FromResult(1));
        await Task.Delay(50);
        await cache.GetOrAddAsync("Receiving", "Orders", new Shared.Entities.DataGridIntent { Take = 10 }, () => Task.FromResult(2));
        await Task.Delay(50);

        // A replaced entry must still be tracked, or Invalidate could not find it.
        Assert.Single(store.KnownKeys);

        store.Invalidate("Receiving");

        var calls = 0;
        await cache.GetOrAddAsync("Receiving", "Orders", new Shared.Entities.DataGridIntent { Take = 10 }, () => { calls++; return Task.FromResult(3); });

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Scalar_arguments_are_part_of_the_key()
    {
        var store = new GridCacheStore(100, TimeSpan.FromSeconds(60));
        var cache = new GridResultCache(store, null, new StubCurrentUserService(Guid.NewGuid()));
        var calls = 0;

        Task<int> Factory()
        {
            calls++;
            return Task.FromResult(calls);
        }

        var first = await cache.GetOrAddAsync("Items", "ByLocation", null, Factory, default, 10);
        var again = await cache.GetOrAddAsync("Items", "ByLocation", null, Factory, default, 10);
        var otherLocation = await cache.GetOrAddAsync("Items", "ByLocation", null, Factory, default, 20);

        Assert.Equal(first, again);
        Assert.NotEqual(first, otherLocation);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task Invalidate_evicts_every_page_for_the_entity()
    {
        var store = new GridCacheStore(100, TimeSpan.FromSeconds(60));
        var cache = new GridResultCache(store, null, new StubCurrentUserService(Guid.NewGuid()));

        await cache.GetOrAddAsync("Receiving", "Orders", new Shared.Entities.DataGridIntent { Take = 10 }, () => Task.FromResult(1));
        await cache.GetOrAddAsync("SupplierReturn", "Returns", new Shared.Entities.DataGridIntent { Take = 10 }, () => Task.FromResult(2));

        store.Invalidate("Receiving");

        var calls = 0;
        await cache.GetOrAddAsync("Receiving", "Orders", new Shared.Entities.DataGridIntent { Take = 10 }, () => { calls++; return Task.FromResult(3); });
        await cache.GetOrAddAsync("SupplierReturn", "Returns", new Shared.Entities.DataGridIntent { Take = 10 }, () => { calls++; return Task.FromResult(4); });

        Assert.Equal(1, calls);
    }

    private sealed class StubCurrentUserService(Guid userId) : Shared.Services.Repository.ICurrentUserService
    {
        public Guid UserId => userId;
        public string UserName => "test";
        public int NsSubsidiaryId => 1;
        public void SetUser(Guid userId, string userName, int nsSubsidiaryId) { }
        public void SetUser(Guid userId, string userName) { }
        public void SetUser(Guid userId) { }
    }
}