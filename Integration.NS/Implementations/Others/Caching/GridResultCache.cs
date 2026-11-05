using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Shared.Entities;
using Shared.Services.Repository;

namespace Integration.NS.Implementations.Others.Caching;

/// <summary>
/// Process-wide storage for live grid page results. Owns a dedicated
/// <see cref="MemoryCache"/> whose <see cref="MemoryCacheOptions.SizeLimit"/>
/// bounds the entry count; the backing cache evicts least-recently-used entries
/// when the limit is reached (Req 2.3).
///
/// Kept separate from <see cref="GridResultCache"/> because the backing cache
/// and its key index must be shared process-wide (singleton) while the caller
/// identity used for scoping is per Blazor circuit (scoped).
/// </summary>
public sealed class GridCacheStore : IDisposable
{
    private const string PrefixRoot = "ns:grid:v1:";

    /// <summary>
    /// Fixed number of single-flight gates. Gates are striped by key hash rather
    /// than allocated per key: a per-key semaphore dictionary grows by one entry
    /// for every distinct grid intent ever seen and is never released, so it
    /// leaks for the lifetime of the process. Unrelated keys that share a gate
    /// serialise against each other, which stays correct and costs nothing at
    /// realistic concurrency.
    /// </summary>
    private const int GateCount = 64;

    private readonly MemoryCache _cache;
    private readonly ConcurrentDictionary<string, byte> _knownKeys = new();
    private readonly SemaphoreSlim[] _gates;

    public GridCacheStore(int maxEntries, TimeSpan ttl)
    {
        _cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = maxEntries });
        Ttl = ttl;
        _gates = Enumerable.Range(0, GateCount).Select(_ => new SemaphoreSlim(1, 1)).ToArray();
    }

    public TimeSpan Ttl { get; }

    internal static string PrefixFor(string entity) => $"{PrefixRoot}{entity}:";

    internal MemoryCache Cache => _cache;

    /// <summary>Current entry count; compaction is asynchronous.</summary>
    internal int Count => Cache.Count;

    internal ConcurrentDictionary<string, byte> KnownKeys => _knownKeys;

    /// <summary>
    /// Returns the single-flight gate for <paramref name="key"/>. Callers must
    /// release the returned semaphore; it is never disposed or removed because
    /// the array that owns it is bounded.
    /// </summary>
    internal SemaphoreSlim GateFor(string key) => _gates[(uint)key.GetHashCode() % GateCount];

    /// <summary>
    /// Drops <paramref name="key"/> from <see cref="KnownKeys"/> when the backing
    /// cache evicts it. Registered on every entry so the index cannot outlive
    /// the entries it tracks: without this, TTL expiry and
    /// <see cref="MemoryCacheOptions.SizeLimit"/> compaction would leave the key
    /// index growing forever while the cache itself stays capped.
    ///
    /// <see cref="EvictionReason.Replaced"/> is deliberately ignored: the entry
    /// still exists under that key, and <c>MemoryCache</c> raises the callback on
    /// a background task, so honouring it would drop a live key from the index
    /// and strand the cached value beyond <see cref="Invalidate"/>. The caller
    /// re-adds the key on the write path instead.
    /// </summary>
    internal void ForgetKey(object key, object? value, EvictionReason reason, object? state)
    {
        if (reason == EvictionReason.Replaced)
        {
            return;
        }

        if (key is string cacheKey)
        {
            KnownKeys.TryRemove(cacheKey, out _);
        }
    }

    /// <summary>
    /// Evicts every cached grid page stored for <paramref name="entity"/>, for
    /// all users (Req 2.2).
    /// </summary>
    public void Invalidate(string entity)
    {
        var prefix = PrefixFor(entity);
        foreach (var key in KnownKeys.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToList())
        {
            Remove(key);
        }
    }

    internal void Remove(string key)
    {
        Cache.Remove(key);
        KnownKeys.TryRemove(key, out _);
    }

    public void Dispose() => Cache.Dispose();
}

/// <summary>
/// Short-TTL (60s) cache for live grid page results, scoped to the calling
/// user. Entries are keyed by entity + method + user + serialized
/// <see cref="DataGridIntent"/>, so pagination/sort/filter combinations never
/// collide and no grid page can be served to another user (Req 2.4).
///
/// If the caller cannot be identified the cache is bypassed entirely rather
/// than risking cross-user leakage.
/// </summary>
public sealed class GridResultCache
{
    private const string EmployeeIdClaim = "com.direcbusiness.wms.nsEmployeeId";

    private readonly GridCacheStore _store;
    private readonly IHttpContextAccessor? _accessor;
    private readonly ICurrentUserService? _currentUser;

    public GridResultCache(
        GridCacheStore store,
        IHttpContextAccessor? accessor,
        ICurrentUserService? currentUser)
    {
        _store = store;
        _accessor = accessor;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Returns the cached grid page, invoking <paramref name="factory"/> once
    /// on a miss. Concurrent misses for the same key collapse into a single
    /// factory call. Exceptions are never cached.
    /// </summary>
    public Task<T> GetOrAddAsync<T>(
        string entity,
        string method,
        DataGridIntent? intent,
        Func<Task<T>> factory,
        CancellationToken ct = default,
        params object?[] scalars)
    {
        if (TryGetUserScope(out var scope) is false)
        {
            return factory();
        }

        var key = BuildKey(entity, method, scope, intent, scalars);

        if (_store.Cache.TryGetValue(key, out CachedGridValue<T>? cached) && cached is not null)
        {
            return Task.FromResult(cached.Value);
        }

        var gate = _store.GateFor(key);
        return ExecuteAsync(key, gate, factory, ct);
    }

    /// <summary>
    /// Drops every cached page for <paramref name="entity"/>. Called by the
    /// decorators after a successful mutation so the next grid load refetches.
    /// </summary>
    public void Invalidate(string entity) => _store.Invalidate(entity);

    private async Task<T> ExecuteAsync<T>(string key, SemaphoreSlim gate, Func<Task<T>> factory, CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            if (_store.Cache.TryGetValue(key, out CachedGridValue<T>? cached) && cached is not null)
            {
                return cached.Value;
            }

            ct.ThrowIfCancellationRequested();
            var value = await factory();
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _store.Ttl,
                Size = 1,
            };
            // Keeps KnownKeys in step with the cache on every eviction path
            // (TTL expiry, SizeLimit compaction, explicit remove/replace).
            options.RegisterPostEvictionCallback(_store.ForgetKey);
            _store.Cache.Set(key, new CachedGridValue<T>(value), options);
            _store.KnownKeys.TryAdd(key, 0);
            return value;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// Key shape: <c>ns:grid:v1:{entity}:{method}:{user}:{hash}</c> — entity
    /// first so <see cref="GridCacheStore.Invalidate"/> can evict by prefix.
    /// </summary>
    internal static string BuildKey(
        string entity,
        string method,
        string userScope,
        DataGridIntent? intent,
        object?[] scalars)
    {
        var payload = new StringBuilder();
        payload.Append("intent=");
        payload.Append(intent is null ? "null" : JsonSerializer.Serialize(intent));
        payload.Append(";params=");
        payload.Append(scalars.Length == 0
            ? string.Empty
            : string.Join(",", scalars.Select(value =>
                value is null ? "null" : JsonSerializer.Serialize(value, value.GetType()))));

        var hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(payload.ToString())))
            .ToLowerInvariant();

        return $"{GridCacheStore.PrefixFor(entity)}{method}:{userScope}:{hash}";
    }

    /// <summary>
    /// Resolves the caller identity used to scope grid entries, combining the
    /// scoped current-user service (populated for the whole Blazor circuit)
    /// with the <c>nsEmployeeId</c> claim when the HTTP context is available.
    /// </summary>
    private bool TryGetUserScope(out string scope)
    {
        var employeeId = _accessor?.HttpContext?.User?.FindFirst(EmployeeIdClaim)?.Value;

        if (_currentUser is not null && _currentUser.UserId != Guid.Empty)
        {
            // NsSubsidiaryId is part of the scope because a user can switch the
            // subsidiary mid-session and the underlying queries filter on it.
            scope = string.IsNullOrEmpty(employeeId)
                ? $"{_currentUser.UserId}:{_currentUser.NsSubsidiaryId}"
                : $"{_currentUser.UserId}:{_currentUser.NsSubsidiaryId}:{employeeId}";
            return true;
        }

        if (!string.IsNullOrEmpty(employeeId))
        {
            scope = $"ns:{employeeId}";
            return true;
        }

        scope = string.Empty;
        return false;
    }
}

/// <summary>
/// Wrapper so a cached <c>null</c> grid result is still a cache hit
/// (<see cref="IMemoryCache.TryGetValue{TItem}"/> reports a stored null as a
/// miss).
/// </summary>
internal sealed record CachedGridValue<T>(T Value);