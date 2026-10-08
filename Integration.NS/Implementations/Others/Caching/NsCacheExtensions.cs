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
    /// Returns the cached value when present, otherwise invokes
    /// <paramref name="factory"/> once and stores the result for
    /// <paramref name="ttl"/>.
    /// </summary>
    public static async Task<T> GetOrAddAsync<T>(
        this IMemoryCache cache,
        string key,
        TimeSpan ttl,
        Func<Task<T>> factory)
    {
        if (cache.TryGetValue(key, out CachedValue<T>? cached) && cached is not null)
        {
            return cached.Value;
        }

        var value = await factory();
        cache.Set(key, new CachedValue<T>(value), ttl);
        return value;
    }
}
