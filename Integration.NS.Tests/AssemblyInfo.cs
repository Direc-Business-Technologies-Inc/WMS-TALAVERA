using System.Runtime.CompilerServices;
using Integration.NS.Services;
using Xunit;

// The token/key tests mutate process environment variables and read static
// state on NetSuiteApiClientService; run the suite sequentially so those
// tests can't race each other.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Integration.NS.Tests;

internal static class TestEnvironment
{
    /// <summary>
    /// NetSuiteApiClientService captures ACCOUNT_ID into static readonly fields
    /// at type initialization (building its request URLs). Set a value before
    /// any test touches the type so those URLs are parseable.
    /// </summary>
    [ModuleInitializer]
    internal static void Initialize()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ACCOUNT_ID")))
        {
            Environment.SetEnvironmentVariable("ACCOUNT_ID", "WMS_TEST");
        }

        // Force the type initializer now, before any test mutates the limiter
        // environment variable, so the shared semaphore is deterministically
        // sized from the default (2) regardless of test order.
        RuntimeHelpers.RunClassConstructor(typeof(NetSuiteApiClientService).TypeHandle);
    }
}
