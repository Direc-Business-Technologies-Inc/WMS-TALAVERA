namespace Integration.NS.Exceptions;

/// <summary>
/// NetSuite business/validation failure (4xx other than 408/429). Never retried.
/// The message preserves the existing BuildNetSuiteErrorMessage output; the
/// parsed detail is also exposed separately for callers that need it.
/// <see cref="ErrorCode"/> carries the stable classification — branch on it,
/// never on the message text (NetSuite rewords detail strings without notice).
/// </summary>
public class NetSuiteValidationException : Exception
{
    public string? Detail { get; }
    public NetSuiteErrorCode ErrorCode { get; }

    public NetSuiteValidationException(string message, string? detail = null, NetSuiteErrorCode errorCode = NetSuiteErrorCode.Unknown)
        : base(message)
    {
        Detail = detail;
        ErrorCode = errorCode;
    }

    public NetSuiteValidationException(string message, string? detail, Exception innerException, NetSuiteErrorCode errorCode = NetSuiteErrorCode.Unknown)
        : base(message, innerException)
    {
        Detail = detail;
        ErrorCode = errorCode;
    }
}
