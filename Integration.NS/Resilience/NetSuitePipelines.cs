using System.Net;
using Application.DataTransferObjects.Others.NS;
using Integration.NS.Exceptions;
using Polly;
using Polly.Retry;

namespace Integration.NS.Resilience;

/// <summary>
/// Cached Polly v8 pipelines for the interactive retry profile.
/// <para>
/// Reads retry any <see cref="NetSuiteTransientException"/>; writes retry only
/// when NetSuite rejected the call for concurrency (429). A 429 guarantees the
/// write never executed, making it the only duplicate-safe retry without a
/// durable write ledger.
/// </para>
/// </summary>
public static class NetSuitePipelines
{
    private const int MaxRetryAttempts = 1;

    private static TimeSpan _retryDelay = TimeSpan.FromSeconds(5);

    private static ResiliencePipeline _interactiveRead = BuildRead();
    private static ResiliencePipeline _interactiveWrite = BuildWrite();

    /// <summary>Pipeline that retries any transient failure (reads).</summary>
    public static ResiliencePipeline InteractiveRead => _interactiveRead;

    /// <summary>Pipeline that retries 429 only (writes).</summary>
    public static ResiliencePipeline InteractiveWrite => _interactiveWrite;

    public static ResiliencePipeline For(NetSuiteRetryProfile profile, NetSuiteRequestKind kind)
    {
        _ = profile; // Only Interactive exists in this phase; Worker is reserved.

        return kind == NetSuiteRequestKind.Read
            ? _interactiveRead
            : _interactiveWrite;
    }

    /// <summary>
    /// Test seam: shorten the backoff so retry tests do not wait ~5 s. Not used
    /// by production code (default is the spec's 5 s jittered delay).
    /// </summary>
    internal static void OverrideRetryDelayForTests(TimeSpan delay)
    {
        _retryDelay = delay;
        _interactiveRead = BuildRead();
        _interactiveWrite = BuildWrite();
    }

    internal static void ResetRetryDelayForTests()
    {
        _retryDelay = TimeSpan.FromSeconds(5);
        _interactiveRead = BuildRead();
        _interactiveWrite = BuildWrite();
    }

    private static ResiliencePipeline BuildRead() =>
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<NetSuiteTransientException>(),
                MaxRetryAttempts = MaxRetryAttempts,
                Delay = _retryDelay,
                BackoffType = DelayBackoffType.Constant,
                UseJitter = true
            })
            .Build();

    private static ResiliencePipeline BuildWrite() =>
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<NetSuiteTransientException>(
                        ex => ex.StatusCode == HttpStatusCode.TooManyRequests),
                MaxRetryAttempts = MaxRetryAttempts,
                Delay = _retryDelay,
                BackoffType = DelayBackoffType.Constant,
                UseJitter = true
            })
            .Build();
}
