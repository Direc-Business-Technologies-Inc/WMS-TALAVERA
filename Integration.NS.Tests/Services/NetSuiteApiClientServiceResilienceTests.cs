using System.Net;
using System.Reflection;
using Application.DataTransferObjects.Others.NS;
using Integration.NS.Exceptions;
using Integration.NS.Resilience;
using Integration.NS.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Integration.NS.Tests.Services;

/// <summary>
/// Behavioral tests for the unified limiter and write-safe retry, driven
/// against a WireMock fake NetSuite (scripted statuses) and hand-rolled
/// concurrency handlers.
/// </summary>
public class NetSuiteApiClientServiceResilienceTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly NetSuiteApiClientService _service;

    private static readonly FieldInfo AccessTokenField =
        typeof(NetSuiteApiClientService).GetField("_accessToken", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly FieldInfo TokenExpiryField =
        typeof(NetSuiteApiClientService).GetField("_tokenExpiryTime", BindingFlags.NonPublic | BindingFlags.Static)!;

    public NetSuiteApiClientServiceResilienceTests()
    {
        NetSuitePipelines.OverrideRetryDelayForTests(TimeSpan.FromMilliseconds(1));

        _server = WireMockServer.Start();
        NetSuiteApiClientService.OverrideHttpClientForTests(_server.CreateClient());
        NetSuiteApiClientService.OverrideUrlRootsForTests(
            suiteQlRoot: _server.Url + "/query/v1/suiteql",
            recordApiRoot: _server.Url + "/record/v1");

        // Pre-seed a valid token so no token refresh HTTP call is attempted.
        AccessTokenField.SetValue(null, "test-token");
        TokenExpiryField.SetValue(null, DateTime.UtcNow.AddMinutes(10));

        _service = new NetSuiteApiClientService(
            new HttpContextAccessor(),
            null!,
            NullLogger<NetSuiteApiClientService>.Instance);
    }

    public void Dispose()
    {
        _server.Stop();
        NetSuiteApiClientService.ResetUrlRootsForTests();
        NetSuitePipelines.ResetRetryDelayForTests();
    }

    // AC2 / property 3 — a write that gets 429 is retried once and succeeds.
    [Fact]
    public async Task Write_429_is_retried_once_and_succeeds()
    {
        const string path = "/record/v1/order/1";

        _server.Given(Request.Create().WithPath(path).UsingPost())
            .InScenario("write429")
            .WillSetStateTo("ok")
            .RespondWith(Response.Create().WithStatusCode(429).WithBody("{\"o:errorDetails\":[{\"detail\":\"Too many requests\"}]}"));

        _server.Given(Request.Create().WithPath(path).UsingPost())
            .InScenario("write429")
            .WhenStateIs("ok")
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("{}"));

        var result = await _service.MakeRequest<object>(_server.Url + path, "{}", HttpMethod.Post);

        Assert.NotNull(result);
        Assert.Equal(2, _server.LogEntries.Count());
    }

    // AC3 / property 4 — a write that gets 5xx is NOT retried.
    [Fact]
    public async Task Write_5xx_is_not_retried()
    {
        const string path = "/record/v1/order/2";

        _server.Given(Request.Create().WithPath(path).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(500).WithBody("server exploded"));

        var ex = await Assert.ThrowsAsync<NetSuiteTransientException>(() =>
            _service.MakeRequest<object>(_server.Url + path, "{}", HttpMethod.Post));

        Assert.Equal(HttpStatusCode.InternalServerError, ex.StatusCode);
        Assert.Single(_server.LogEntries);
    }

    // AC4 / property 5 — a read that gets 5xx is retried once.
    [Fact]
    public async Task Read_5xx_is_retried_once()
    {
        const string path = "/query/v1/suiteql";

        _server.Given(Request.Create().WithPath(path).UsingPost())
            .InScenario("read500")
            .WillSetStateTo("ok")
            .RespondWith(Response.Create().WithStatusCode(500).WithBody("boom"));

        _server.Given(Request.Create().WithPath(path).UsingPost())
            .InScenario("read500")
            .WhenStateIs("ok")
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("{\"items\":[],\"totalResults\":0}"));

        var result = await _service.MakeRequest<NetSuiteResponse<object>>(
            _server.Url + path,
            "{\"q\":\"SELECT 1\"}",
            HttpMethod.Post);

        Assert.NotNull(result);
        Assert.Equal(2, _server.LogEntries.Count());
    }

    // AC5 / property 6 — a validation error is never retried and keeps its detail.
    [Fact]
    public async Task Validation_400_is_not_retried()
    {
        const string path = "/record/v1/order/3";

        _server.Given(Request.Create().WithPath(path).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(400)
                .WithBody("{\"o:errorDetails\":[{\"detail\":\"Insufficient stock\"}]}"));

        var ex = await Assert.ThrowsAsync<NetSuiteValidationException>(() =>
            _service.MakeRequest<object>(_server.Url + path, "{}", HttpMethod.Post));

        Assert.StartsWith("NetSuite 400 ", ex.Message);
        Assert.Contains("Insufficient stock", ex.Message);
        Assert.NotNull(ex.Detail);
        Assert.Single(_server.LogEntries);
    }

    // Property 4 — a write timeout surfaces immediately (never retried).
    [Fact]
    public async Task Write_timeout_is_not_retried()
    {
        var handler = new ThrowingHandler(() => new TaskCanceledException("timeout"));
        NetSuiteApiClientService.OverrideHttpClientForTests(new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) });

        var ex = await Assert.ThrowsAsync<NetSuiteTransientException>(() =>
            _service.MakeRequest<object>("https://x/record/v1/order/9", "{}", HttpMethod.Post));

        Assert.Null(ex.StatusCode);
        Assert.Equal(1, handler.Attempts);
    }

    // Property 5 — a read timeout is retried once.
    [Fact]
    public async Task Read_timeout_is_retried()
    {
        var handler = new ThrowingHandler(() => new TaskCanceledException("timeout"));
        NetSuiteApiClientService.OverrideHttpClientForTests(new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) });

        await Assert.ThrowsAsync<NetSuiteTransientException>(() =>
            _service.MakeRequest<object>("https://x/query/v1/suiteql", "{}", HttpMethod.Post));

        Assert.Equal(2, handler.Attempts);
    }

    // Req 1.5 / property 8 — a timed-out call releases its permit (no leak).
    [Fact]
    public async Task Timed_out_call_releases_its_permit()
    {
        var semaphore = GetStaticField<SemaphoreSlim>("_requestSemaphore");
        var initialCount = semaphore.CurrentCount;

        var handler = new ThrowingHandler(() => new TaskCanceledException("timeout"));
        NetSuiteApiClientService.OverrideHttpClientForTests(new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) });

        await Assert.ThrowsAsync<NetSuiteTransientException>(() =>
            _service.MakeRequest<object>("https://x/record/v1/order/9", "{}", HttpMethod.Post));

        Assert.Equal(initialCount, semaphore.CurrentCount);
    }

    // AC1 / property 1 — at most the configured in-flight cap, observed directly.
    [Fact]
    public async Task Limiter_caps_in_flight_requests()
    {
        var handler = new ConcurrencyHandler(TimeSpan.FromMilliseconds(20));
        NetSuiteApiClientService.OverrideHttpClientForTests(new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) });

        var tasks = Enumerable.Range(0, 30)
            .Select(i => _service.MakeRequest<object>($"https://x/record/v1/order/{i}", "{}", HttpMethod.Post))
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.True(handler.MaxInFlight <= 2, $"observed max in-flight {handler.MaxInFlight}");
        Assert.Equal(30, handler.TotalRequests);
    }

    // Property 2 — the OAuth1 and OAuth2 paths share one limiter.
    [Fact]
    public async Task OAuth1_and_OAuth2_paths_share_one_limiter()
    {
        var handler = new ConcurrencyHandler(TimeSpan.FromMilliseconds(20));
        NetSuiteApiClientService.OverrideHttpClientForTests(new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) });

        var oauth2 = Enumerable.Range(0, 15)
            .Select(i => _service.MakeRequest<object>($"https://x/record/v1/order/{i}", "{}", HttpMethod.Post));

        var oauth1 = Enumerable.Range(0, 15)
            .Select(i => _service.MakeRequestOAuth1<object>($"https://x/restlet?i={i}", "{}", HttpMethod.Post));

        await Task.WhenAll(oauth2.Concat(oauth1));

        Assert.True(handler.MaxInFlight <= 2, $"observed max in-flight {handler.MaxInFlight}");
        Assert.Equal(30, handler.TotalRequests);
    }

    // Req 2.5 — an explicit kind override wins over URL classification.
    [Fact]
    public async Task Explicit_kind_override_wins_over_url_classification()
    {
        const string path = "/query/v1/suiteql";

        _server.Given(Request.Create().WithPath(path).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(500).WithBody("boom"));

        await Assert.ThrowsAsync<NetSuiteTransientException>(() =>
            _service.MakeRequest<object>(_server.Url + path, "{}", HttpMethod.Post, NetSuiteRequestKind.Write));

        // Write => 500 is not retried, so exactly one attempt reaches the fake.
        Assert.Single(_server.LogEntries);
    }

    private static T GetStaticField<T>(string name)
    {
        var field = typeof(NetSuiteApiClientService)
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"Field '{name}' not found.");

        return (T)field.GetValue(null)!;
    }

    private sealed class ThrowingHandler(Func<Exception> exceptionFactory) : HttpMessageHandler
    {
        private int _attempts;

        public int Attempts => Volatile.Read(ref _attempts);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _attempts);
            throw exceptionFactory();
        }
    }

    private sealed class ConcurrencyHandler(TimeSpan latency) : HttpMessageHandler
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
