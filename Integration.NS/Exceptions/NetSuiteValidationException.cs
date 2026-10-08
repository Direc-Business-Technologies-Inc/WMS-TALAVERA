namespace Integration.NS.Exceptions;

/// <summary>
/// NetSuite business/validation failure (4xx other than 408/429). Never retried.
/// The message preserves the existing BuildNetSuiteErrorMessage output; the
/// parsed detail is also exposed separately for callers that need it.
/// </summary>
public class NetSuiteValidationException : Exception
{
    public string? Detail { get; }

    public NetSuiteValidationException(string message, string? detail = null)
        : base(message)
    {
        Detail = detail;
    }

    public NetSuiteValidationException(string message, string? detail, Exception innerException)
        : base(message, innerException)
    {
        Detail = detail;
    }
}
