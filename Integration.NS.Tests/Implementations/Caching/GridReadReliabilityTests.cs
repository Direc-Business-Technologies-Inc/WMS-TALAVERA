using Integration.NS.Implementations.Others.Caching;
using Integration.NS.Tests.Fakes;
using Shared.Entities;
using Xunit;

namespace Integration.NS.Tests.Implementations.Caching;

public class GridReadReliabilityTests
{
    private sealed class StubCurrentUserService(Guid userId, int subsidiaryId = 1) : Shared.Services.Repository.ICurrentUserService
    {
        public Guid UserId => userId;
        public string UserName => "test";
        public int NsSubsidiaryId => subsidiaryId;
        public void SetUser(Guid id, string name, int subsidiaryId) { }
        public void SetUser(Guid id, string name) { }
        public void SetUser(Guid id) { }
    }
    private readonly GridResultCache cache = new(new GridCacheStore(100, TimeSpan.FromMinutes(1)),
        null, new StubCurrentUserService(Guid.NewGuid()));

    [Fact]
    public async Task Fresh_bypasses_completed_entry_and_populates_normal_cache()
    {
        var intent = new DataGridIntent();
        Assert.Equal("old", await cache.ReadAsync("Receiving", "rows", intent, _ => Task.FromResult("old")));
        intent.ReadPolicy = GridReadPolicy.Fresh;
        Assert.Equal("fresh", await cache.ReadAsync("Receiving", "rows", intent, _ => Task.FromResult("fresh")));
        intent.ReadPolicy = GridReadPolicy.Normal;
        Assert.Equal("fresh", await cache.ReadAsync<string>("Receiving", "rows", intent, _ => throw new Exception("cache miss")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Overlapping_reads_share_success_or_failure_but_not_completed_packing_results(bool fail)
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        async Task<int> Read(CancellationToken ct) { Interlocked.Increment(ref calls); entered.TrySetResult(); return await release.Task; }
        var first = cache.ReadAsync("Packing", "rows", new(), Read, cacheCompleted: false);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var second = cache.ReadAsync("Packing", "rows", new(), Read, cacheCompleted: false);
        Assert.Equal(1, calls);
        if (fail)
        {
            release.SetException(new InvalidOperationException("429 exhausted"));
            await Assert.ThrowsAsync<InvalidOperationException>(() => first);
            await Assert.ThrowsAsync<InvalidOperationException>(() => second);
        }
        else
        {
            release.SetResult(42);
            Assert.Equal(new[] { 42, 42 }, await Task.WhenAll(first, second));
        }
        Assert.Equal(1, calls);
        Assert.Equal(99, await cache.ReadAsync("Packing", "rows", new(), _ => Task.FromResult(99), cacheCompleted: false));
    }

    [Fact]
    public async Task Cancelling_one_waiter_does_not_cancel_another()
    {
        using var cancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken upstream = default;
        var first = cache.ReadAsync("Receiving", "rows", new(), async ct =>
        {
            upstream = ct; entered.SetResult(); return await release.Task.WaitAsync(ct);
        }, cancellation.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var second = cache.ReadAsync<int>("Receiving", "rows", new(), _ => throw new Exception("duplicate"));
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        Assert.False(upstream.IsCancellationRequested);
        release.SetResult(7);
        Assert.Equal(7, await second);
    }

    [Fact]
    public async Task Last_waiter_cancellation_cancels_upstream_and_new_waiter_starts_new_work()
    {
        using var cancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = cache.ReadAsync<int>("Receiving", "rows", new(), async ct =>
        {
            entered.SetResult();
            try { await Task.Delay(Timeout.Infinite, ct); return 0; }
            finally { stopped.SetResult(); }
        }, cancellation.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        await stopped.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(8, await cache.ReadAsync("Receiving", "rows", new(), _ => Task.FromResult(8)));
    }

    [Fact]
    public async Task Fresh_and_post_mutation_reads_cannot_join_older_reads()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var normal = cache.ReadAsync("Receiving", "rows", new(), async _ => { entered.SetResult(); return await release.Task; });
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(2, await cache.ReadAsync("Receiving", "rows", new() { ReadPolicy = GridReadPolicy.Fresh }, _ => Task.FromResult(2)));
        cache.Invalidate("Receiving");
        Assert.Equal(3, await cache.ReadAsync("Receiving", "rows", new(), _ => Task.FromResult(3)));
        release.SetResult(1);
        Assert.Equal(1, await normal);
        Assert.Equal(3, await cache.ReadAsync("Receiving", "rows", new(), _ => Task.FromResult(4)));
    }

    [Fact]
    public async Task Older_normal_completion_cannot_overwrite_newer_fresh_cache_result()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var normal = cache.ReadAsync("Receiving", "rows", new(), async _ => { entered.SetResult(); return await release.Task; });
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(2, await cache.ReadAsync("Receiving", "rows", new() { ReadPolicy = GridReadPolicy.Fresh }, _ => Task.FromResult(2)));
        release.SetResult(1);
        await normal;
        Assert.Equal(2, await cache.ReadAsync("Receiving", "rows", new(), _ => Task.FromResult(3)));
    }

    [Theory]
    [InlineData("user")]
    [InlineData("subsidiary")]
    [InlineData("authorization")]
    public async Task Protected_scopes_do_not_share_overlapping_reads_in_the_same_store(string boundary)
    {
        using var store = new GridCacheStore(100, TimeSpan.FromMinutes(1));
        var userId = Guid.NewGuid();
        Microsoft.AspNetCore.Http.IHttpContextAccessor Accessor(string locations) => new Microsoft.AspNetCore.Http.HttpContextAccessor
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity([
                    new System.Security.Claims.Claim("com.direcbusiness.wms.nsAllowedLocations", locations)
                ], "test"))
            }
        };
        var firstCache = new GridResultCache(store, Accessor("1"), new StubCurrentUserService(userId));
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = firstCache.ReadAsync("Packing", "rows", new(), async _ => { entered.SetResult(); return await release.Task; }, cacheCompleted: false);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var secondCache = new GridResultCache(store, Accessor(boundary == "authorization" ? "2" : "1"),
            new StubCurrentUserService(boundary == "user" ? Guid.NewGuid() : userId, boundary == "subsidiary" ? 2 : 1));
        Assert.Equal(2, await secondCache.ReadAsync("Packing", "rows", new(), _ => Task.FromResult(2), cacheCompleted: false).WaitAsync(TimeSpan.FromSeconds(5)));
        release.SetResult(1);
        Assert.Equal(1, await first);
    }
}
