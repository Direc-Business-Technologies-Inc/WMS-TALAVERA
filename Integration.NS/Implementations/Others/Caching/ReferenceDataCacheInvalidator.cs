using Microsoft.Extensions.Caching.Memory;

namespace Integration.NS.Implementations.Others.Caching;

/// <summary>
/// Evicts cached reference-data entries. There are currently no mutation
/// commands for these read-only entities (warehouses, school years, transfer
/// types, transaction types, departments come from NetSuite/SAP), so nothing
/// calls this yet — it exists so the first mutation handler can invalidate
/// without touching the decorators.
/// </summary>
public interface IReferenceDataCacheInvalidator
{
    /// <summary>
    /// Removes all cached entries previously stored under
    /// <c>ns:cache:v1:{interfaceName}:</c>.
    /// </summary>
    void Invalidate(string interfaceName);
}

public sealed class ReferenceDataCacheInvalidator(IMemoryCache cache) : IReferenceDataCacheInvalidator
{
    private const string PrefixRoot = "ns:cache:v1:";

    public void Invalidate(string interfaceName)
    {
        var prefix = $"{PrefixRoot}{interfaceName}:";
        foreach (var key in NsCacheExtensions.KeysWithPrefix(prefix))
        {
            cache.Remove(key);
            NsCacheExtensions.ForgetKey(key);
        }
    }
}

/// <summary>
/// Cache-key prefix helpers shared by the reference-data decorators.
/// </summary>
internal static class NsCacheInvalidation
{
    internal static void RemoveByPrefix(IMemoryCache cache, string prefix)
    {
        foreach (var key in NsCacheExtensions.KeysWithPrefix(prefix))
        {
            cache.Remove(key);
            NsCacheExtensions.ForgetKey(key);
        }
    }
}
