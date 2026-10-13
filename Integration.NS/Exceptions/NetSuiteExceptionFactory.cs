using System.Net;

namespace Integration.NS.Exceptions;

/// <summary>
/// Single place that maps a NetSuite HTTP outcome to a typed exception. Keeping
/// the classification here (rather than at each throw site) makes the transient
/// vs. validation decision unit-testable and consistent across both request
/// paths (OAuth2 <c>MakeRequest</c> and OAuth1 <c>MakeRequestOAuth1</c>).
/// </summary>
public static class NetSuiteExceptionFactory
{
    /// <summary>
    /// HTTP 408 (Request Timeout), 429 (Too Many Requests) and every 5xx are
    /// considered transient and retryable. All other non-success responses are
    /// treated as validation/business failures and are never retried.
    /// </summary>
    public static bool IsTransientStatus(HttpStatusCode statusCode)
    {
        if (statusCode == HttpStatusCode.RequestTimeout ||
            statusCode == HttpStatusCode.TooManyRequests)
        {
            return true;
        }

        var code = (int)statusCode;
        return code >= 500 && code <= 599;
    }

    /// <summary>
    /// Builds the typed exception for a non-success NetSuite response. The
    /// <paramref name="message"/> is passed through unchanged so the existing
    /// user-visible error strings are preserved (Req 4.2).
    /// <para>
    /// When the failure is an unsupported SuiteQL query shape and
    /// <paramref name="requestBody"/> is supplied, a truncated excerpt of the
    /// failing query is appended so the shape that broke is visible from the
    /// toast/log without grepping the request-body logs. Everything else keeps
    /// the message byte-identical (existing call sites and tests depend on it).
    /// </para>
    /// </summary>
    public static Exception CreateForStatus(
        HttpStatusCode statusCode,
        string message,
        string? detail = null,
        string? requestBody = null)
    {
        if (IsTransientStatus(statusCode))
        {
            return new NetSuiteTransientException(message, statusCode);
        }

        var errorCode = NetSuiteErrorCodes.Classify(message, detail);

        if (errorCode == NetSuiteErrorCode.UnsupportedSearch)
        {
            message = AppendQueryExcerpt(message, requestBody);
        }

        return new NetSuiteValidationException(message, detail, errorCode);
    }

    internal const int QueryExcerptLimit = 500;

    private static string AppendQueryExcerpt(string message, string? requestBody)
    {
        if (string.IsNullOrWhiteSpace(requestBody))
        {
            return message;
        }

        var query = TryExtractSuiteQL(requestBody) ?? requestBody;

        var excerpt = query.Length <= QueryExcerptLimit
            ? query
            : $"{query[..QueryExcerptLimit]} …(truncated)";

        return $"{message}\nFailing query (excerpt): {excerpt}";
    }

    /// <summary>
    /// The SuiteQL request body is <c>{"q":"..."}</c>; extracting the query text
    /// beats excerpting the raw JSON (which spends the budget on escaping noise).
    /// </summary>
    private static string? TryExtractSuiteQL(string requestBody)
    {
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(requestBody);
            return document.RootElement.TryGetProperty("q", out var query) &&
                   query.ValueKind == System.Text.Json.JsonValueKind.String
                ? query.GetString()
                : null;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Wraps a transport/network failure as transient (Req 4.1).
    /// </summary>
    public static NetSuiteTransientException CreateForNetworkError(
        string message,
        Exception innerException)
    {
        return new NetSuiteTransientException(message, innerException);
    }
}
