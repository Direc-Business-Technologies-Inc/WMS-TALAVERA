using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace Integration.NS.Implementations.Others.Caching;

/// <summary>
/// Wrapper so a cached <c>null</c> reference-read result is still a cache hit
/// (<see cref="IMemoryCache.TryGetValue{TItem}"/> reports a stored null as a
/// miss).
/// </summary>
internal sealed record CachedValue<T>(T Value);

internal static class NsCacheExtensions
{
    /// <summary>
    /// Fixed number of single-flight gates, striped by key hash. Gates are not
    /// allocated per key because a per-key dictionary would grow by one entry
    /// for every distinct reference key ever seen and never release it. Keys
    /// sharing a gate serialise against each other, which is harmless here.
    /// </summary>
    private const int GateCount = 64;

    private static readonly SemaphoreSlim[] Gates =
        Enumerable.Range(0, GateCount).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

    /// <summary>
    /// Tracks every key inserted through this helper so prefix-based
    /// invalidation (<see cref="NsCacheInvalidation.RemoveByPrefix"/>) can
    /// evict without enumerating the cache (which <see cref="IMemoryCache"/>
    /// does not support). Entries are removed by
    /// <see cref="ForgetKey(object, object?, EvictionReason, object?)"/> on
    /// every eviction path, so this index cannot outgrow the cache it tracks.
    /// </summary>
    private static readonly ConcurrentDictionary<string, byte> KnownKeys = new();

    private static SemaphoreSlim GateFor(string key) => Gates[(uint)key.GetHashCode() % GateCount];

    /// <summary>
    /// Returns the cached value when present, otherwise invokes
    /// <paramref name="factory"/> once and stores the result for
    /// <paramref name="ttl"/>.
    /// Concurrent misses for the same <paramref name="key"/> are collapsed
    /// into a single factory invocation (single-flight). Exceptions from the
    /// factory are never cached.
    /// </summary>
    public static async Task<T> GetOrAddAsync<T>(
        this IMemoryCache cache,
        string key,
        TimeSpan ttl,
        Func<Task<T>> factory,
        CancellationToken ct = default)
    {
        if (cache.TryGetValue(key, out CachedValue<T>? cached) && cached is not null)
        {
            return cached.Value;
        }

        var gate = GateFor(key);
        await gate.WaitAsync(ct);
        try
        {
            if (cache.TryGetValue(key, out cached) && cached is not null)
            {
                return cached.Value;
            }

            ct.ThrowIfCancellationRequested();
            var value = await factory();
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl,
            };
            // Keeps KnownKeys in step with the cache on every eviction path
            // (TTL expiry, SizeLimit compaction, explicit remove/replace).
            options.RegisterPostEvictionCallback(ForgetKey);
            cache.Set(key, new CachedValue<T>(value), options);
            KnownKeys.TryAdd(key, 0);
            return value;
        }
        finally
        {
            gate.Release();
        }
    }

    internal static IEnumerable<string> KeysWithPrefix(string prefix) =>
        KnownKeys.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal));

    internal static void ForgetKey(string key) => KnownKeys.TryRemove(key, out _);

    /// <summary>
    /// Post-eviction callback: drops the key from <see cref="KnownKeys"/> when
    /// the cache evicts it. Without this the index would grow forever, because
    /// TTL expiry and <c>SizeLimit</c> compaction never touch it. Matches
    /// <c>EvictionCallbackDelegate</c>.
    ///
    /// <see cref="EvictionReason.Replaced"/> is ignored on purpose — the entry
    /// still exists under that key and the callback is raised on a background
    /// task, so honouring it would strand a live cached value outside
    /// prefix-based invalidation.
    /// </summary>
    private static void ForgetKey(object key, object? value, EvictionReason reason, object? state)
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
}
