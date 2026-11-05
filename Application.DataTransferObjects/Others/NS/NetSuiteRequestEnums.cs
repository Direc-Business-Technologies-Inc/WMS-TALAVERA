namespace Application.DataTransferObjects.Others.NS;

/// <summary>
/// Whether a NetSuite-bound request is safe to retry. Reads never mutate, so
/// they may be retried on any transient failure; writes may only be retried on
/// 429 (concurrency rejection, which guarantees the write never executed).
/// </summary>
public enum NetSuiteRequestKind
{
    Read = 0,
    Write = 1
}

/// <summary>
/// Retry behaviour profile. Only the interactive profile exists in this phase;
/// a Worker profile is reserved for the parent concurrency-optimization spec.
/// </summary>
public enum NetSuiteRetryProfile
{
    Interactive = 0
}
