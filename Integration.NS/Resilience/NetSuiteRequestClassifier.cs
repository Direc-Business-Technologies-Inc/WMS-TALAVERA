using System.Net.Http;
using Application.DataTransferObjects.Others.NS;

namespace Integration.NS.Resilience;

/// <summary>
/// Classifies a NetSuite-bound request as a read (never mutates, always safe to
/// retry) or a write (mutating, retry on 429 only). SuiteQL reads use POST, so
/// classification keys off the URL first and the HTTP method second.
/// </summary>
public static class NetSuiteRequestClassifier
{
    private const string SuiteQlPathSegment = "/query/v1/";

    public static NetSuiteRequestKind Classify(string url, HttpMethod method)
    {
        if (method == HttpMethod.Get)
        {
            return NetSuiteRequestKind.Read;
        }

        if (!string.IsNullOrEmpty(url) &&
            url.Contains(SuiteQlPathSegment, StringComparison.OrdinalIgnoreCase))
        {
            return NetSuiteRequestKind.Read;
        }

        return NetSuiteRequestKind.Write;
    }
}
