using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Shared.Entities;

namespace Integration.NS.Implementations.Others.Caching;

/// <summary>
/// Builds the interim per-process cache key. Shape matches the parent spec D7
/// (<c>ns:cache:v{version}:{interface}:{method}:{hash}</c>) so swapping the
/// in-memory store for Redis later is mechanical.
/// </summary>
public static class NsCacheKey
{
    private const string Version = "v1";

    public static string Build(
        string interfaceName,
        string method,
        DataGridIntent? intent = null,
        string? employeeScope = null,
        params object?[] scalarParameters)
    {
        var payload = new StringBuilder();
        payload.Append("intent=");
        payload.Append(intent is null ? "null" : JsonSerializer.Serialize(intent));
        payload.Append(";scope=");
        payload.Append(employeeScope ?? "null");

        if (scalarParameters.Length > 0)
        {
            payload.Append(";params=");
            payload.Append(string.Join(
                ",",
                scalarParameters.Select(value =>
                    value is null
                        ? "null"
                        : JsonSerializer.Serialize(value, value.GetType()))));
        }

        var hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(payload.ToString())));

        return $"ns:cache:{Version}:{interfaceName}:{method}:{hash.ToLowerInvariant()}";
    }
}
