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
    /// </summary>
    public static Exception CreateForStatus(
        HttpStatusCode statusCode,
        string message,
        string? detail = null)
    {
        return IsTransientStatus(statusCode)
            ? new NetSuiteTransientException(message, statusCode)
            : new NetSuiteValidationException(message, detail, NetSuiteErrorCodes.Classify(message, detail));
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
