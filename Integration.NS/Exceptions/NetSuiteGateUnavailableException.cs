namespace Integration.NS.Exceptions;

/// <summary>
/// Thrown when the shared concurrency gate is unavailable (backend down) or the
/// bounded wait elapsed without a permit. Fail-closed: the NetSuite call does not
/// proceed ungated.
/// </summary>
public class NetSuiteGateUnavailableException : Exception
{
    public NetSuiteGateUnavailableException(string message) : base(message)
    {
    }

    public NetSuiteGateUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
