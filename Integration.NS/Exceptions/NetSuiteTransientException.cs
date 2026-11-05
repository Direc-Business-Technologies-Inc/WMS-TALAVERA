namespace Integration.NS.Exceptions;

using System.Net;

/// <summary>
/// NetSuite failure that is safe to retry: HTTP 408/429, any 5xx response, or a
/// transport/network error. The message preserves the existing
/// BuildNetSuiteErrorMessage output so user-visible detail does not change.
/// </summary>
public class NetSuiteTransientException : Exception
{
    /// <summary>
    /// The HTTP status the transient failure originated from, when known.
    /// Network/transport failures leave this <c>null</c>. The write-safe retry
    /// predicate uses it to retry 429 (concurrency rejection) only.
    /// </summary>
    public HttpStatusCode? StatusCode { get; }

    public NetSuiteTransientException(string message, HttpStatusCode? statusCode = null)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public NetSuiteTransientException(
        string message,
        Exception innerException,
        HttpStatusCode? statusCode = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}
