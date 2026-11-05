using Integration.NS.Services;
using Xunit;

namespace Integration.NS.Tests.Services;

public class NetSuiteApiClientServiceLimiterTests
{
    [Fact]
    public void Max_in_flight_is_env_configurable_with_safe_fallbacks()
    {
        var original = Environment.GetEnvironmentVariable("NETSUITE_MAX_INFLIGHT_PER_PROCESS");
        try
        {
            Environment.SetEnvironmentVariable("NETSUITE_MAX_INFLIGHT_PER_PROCESS", "3");
            Assert.Equal(3, NetSuiteApiClientService.GetMaxInFlightPerProcess());

            Environment.SetEnvironmentVariable("NETSUITE_MAX_INFLIGHT_PER_PROCESS", "not-a-number");
            Assert.Equal(2, NetSuiteApiClientService.GetMaxInFlightPerProcess());

            Environment.SetEnvironmentVariable("NETSUITE_MAX_INFLIGHT_PER_PROCESS", "0");
            Assert.Equal(1, NetSuiteApiClientService.GetMaxInFlightPerProcess());

            Environment.SetEnvironmentVariable("NETSUITE_MAX_INFLIGHT_PER_PROCESS", "-5");
            Assert.Equal(1, NetSuiteApiClientService.GetMaxInFlightPerProcess());

            Environment.SetEnvironmentVariable("NETSUITE_MAX_INFLIGHT_PER_PROCESS", null);
            Assert.Equal(2, NetSuiteApiClientService.GetMaxInFlightPerProcess());
        }
        finally
        {
            Environment.SetEnvironmentVariable("NETSUITE_MAX_INFLIGHT_PER_PROCESS", original);
        }
    }
}
