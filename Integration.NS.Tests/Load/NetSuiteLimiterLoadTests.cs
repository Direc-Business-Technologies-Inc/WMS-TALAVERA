using System.Net;
using System.Reflection;
using Integration.NS.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Integration.NS.Tests.Load;

/// <summary>
/// Burst harness (Req 4.3). Excluded from default runs; execute explicitly with
/// <c>dotnet test --filter "Category=Load"</c>.
/// </summary>
[Trait("Category", "Load")]
public class NetSuiteLimiterLoadTests
{
    private static readonly FieldInfo AccessTokenField =
        typeof(NetSuiteApiClientService).GetField("_accessToken", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly FieldInfo TokenExpiryField =
        typeof(NetSuiteApiClientService).GetField("_tokenExpiryTime", BindingFlags.NonPublic | BindingFlags.Static)!;

    [Fact]
    public async Task Thirty_parallel_writes_observe_the_cap_and_write_exactly_once()
    {
        AccessTokenField.SetValue(null, "test-token");
        TokenExpiryField.SetValue(null, DateTime.UtcNow.AddMinutes(10));

        var handler = new CountingLatencyHandler(TimeSpan.FromMilliseconds(100));
        NetSuiteApiClientService.OverrideHttpClientForTests(new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) });

        try
        {
            var service = new NetSuiteApiClientService(
                new HttpContextAccessor(),
                null!,
                NullLogger<NetSuiteApiClientService>.Instance);

            var tasks = Enumerable.Range(0, 30)
                .Select(i => service.MakeRequest<object>($"https://x/record/v1/order/{i}", "{}", HttpMethod.Post))
                .ToArray();

            await Task.WhenAll(tasks);

            Assert.True(handler.MaxInFlight <= 2, $"observed max in-flight {handler.MaxInFlight}");

            // Each write succeeded on its first (and only) attempt: no duplicates.
            Assert.Equal(30, handler.TotalRequests);
        }
        finally
        {
            NetSuiteApiClientService.ResetUrlRootsForTests();
        }
    }

    private sealed class CountingLatencyHandler(TimeSpan latency) : HttpMessageHandler
    {
        private int _inFlight;
        private int _maxInFlight;
        private int _totalRequests;

        public int MaxInFlight => Volatile.Read(ref _maxInFlight);
        public int TotalRequests => Volatile.Read(ref _totalRequests);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _totalRequests);

            var current = Interlocked.Increment(ref _inFlight);
            UpdateMax(current);

            try
            {
                await Task.Delay(latency, cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}")
                };
            }
            finally
            {
                Interlocked.Decrement(ref _inFlight);
            }
        }

        private void UpdateMax(int current)
        {
            while (true)
            {
                var observed = Volatile.Read(ref _maxInFlight);
                if (current <= observed ||
                    Interlocked.CompareExchange(ref _maxInFlight, current, observed) == observed)
                {
                    break;
                }
            }
        }
    }
}
