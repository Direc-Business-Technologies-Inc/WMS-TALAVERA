namespace Integration.NS.Exceptions;

/// <summary>
/// Stable classification of NetSuite validation failures. NetSuite rewords its
/// human-readable detail strings without notice, so callers that need to branch
/// on a failure kind must switch on <see cref="NetSuiteErrorCode"/> instead of
/// string-matching <see cref="NetSuiteValidationException.Detail"/>.
/// </summary>
public enum NetSuiteErrorCode
{
    /// <summary>Unclassified validation failure; Detail carries the raw text.</summary>
    Unknown = 0,

    /// <summary>
    /// Collection/SuiteQL paging rejected the requested offset — either past the
    /// end of the result set ("offset is out of bounds. Provide value between 0
    /// and N") or not a multiple of the page size. Safe to retry on a legal page.
    /// </summary>
    OffsetOutOfBounds = 1,

    /// <summary>
    /// SuiteQL rejected the query SHAPE itself ("Search error occurred: Invalid
    /// or unsupported search") — typically a correlated subquery referencing the
    /// outer query's aliases. Never retryable; the fix is a query-shape change.
    /// See .opencode/docs/SuiteQL-Shape-Rules.md.
    /// </summary>
    UnsupportedSearch = 2,
}

/// <summary>
/// Maps raw NetSuite error detail text to <see cref="NetSuiteErrorCode"/>.
/// Both the request-level <c>message</c> (built by BuildNetSuiteErrorMessage) and
/// the parsed <c>detail</c> are accepted because some call sites pass a null
/// detail while the message still carries the NetSuite wording.
/// </summary>
public static class NetSuiteErrorCodes
{
    public static NetSuiteErrorCode Classify(string? message, string? detail = null)
    {
        // NetSuite's exact wording drifts ("out of bounds", "out-of-bounds",
        // "must be divisible by the page limit"), so match on the stable tokens
        // rather than the full sentence.
        var normalized = $"{message} {detail}".ToLowerInvariant();

        // Matched first: the unsupported-search wording never carries the offset
        // tokens, and a generic NetSuite wording drift must not re-classify it.
        if (normalized.Contains("invalid or unsupported search"))
        {
            return NetSuiteErrorCode.UnsupportedSearch;
        }

        if (normalized.Contains("offset") &&
            (normalized.Contains("out of bounds") ||
             normalized.Contains("out-of-bounds") ||
             normalized.Contains("divisible")))
        {
            return NetSuiteErrorCode.OffsetOutOfBounds;
        }

        return NetSuiteErrorCode.Unknown;
    }

    /// <summary>
    /// Extracts the legal upper bound N from NetSuite's
    /// "Provide value between 0 and N" hint. Returns null when absent so the
    /// caller can fall back to the first page.
    /// </summary>
    public static int? GetOffsetUpperBound(string? message, string? detail = null)
    {
        var combined = $"{message} {detail}";
        var match = System.Text.RegularExpressions.Regex.Match(
            combined,
            @"between\s+0\s+and\s+(\d+)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success && int.TryParse(match.Groups[1].Value, out var bound)
            ? bound
            : null;
    }
}
