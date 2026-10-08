using System.Net;
using Integration.NS.Exceptions;
using Integration.NS.Resilience;
using Polly;
using Xunit;

namespace Integration.NS.Tests.Resilience;

public class NetSuitePipelinesTests : IDisposable
{
    public NetSuitePipelinesTests()
    {
        // Keep retry tests fast; attempt counts are what matter, not wall time.
        NetSuitePipelines.OverrideRetryDelayForTests(TimeSpan.Zero);
    }

    public void Dispose()
    {
        NetSuitePipelines.ResetRetryDelayForTests();
    }

    private static async Task<int> CountAttempts(ResiliencePipeline pipeline, Exception failure)
    {
        var attempts = 0;

        Func<CancellationToken, ValueTask> callback = _ =>
        {
            attempts++;
            throw failure;
        };

        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await pipeline.ExecuteAsync(callback, CancellationToken.None));

        return attempts;
    }

    [Fact]
    public async Task Write_pipeline_retries_429()
    {
        var attempts = await CountAttempts(
            NetSuitePipelines.InteractiveWrite,
            new NetSuiteTransientException("too many requests", HttpStatusCode.TooManyRequests));

        Assert.Equal(2, attempts);
    }

    [Theory]
    [InlineData(500)]
    [InlineData(503)]
    [InlineData(408)]
    public async Task Write_pipeline_does_not_retry_non_429_statuses(int statusCode)
    {
        var attempts = await CountAttempts(
            NetSuitePipelines.InteractiveWrite,
            new NetSuiteTransientException("transient", (HttpStatusCode)statusCode));

        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task Write_pipeline_does_not_retry_network_failures()
    {
        var attempts = await CountAttempts(
            NetSuitePipelines.InteractiveWrite,
            new NetSuiteTransientException("timed out", new TaskCanceledException()));

        Assert.Equal(1, attempts);
    }

    [Theory]
    [InlineData(429)]
    [InlineData(500)]
    [InlineData(408)]
    public async Task Read_pipeline_retries_any_transient_status(int statusCode)
    {
        var attempts = await CountAttempts(
            NetSuitePipelines.InteractiveRead,
            new NetSuiteTransientException("transient", (HttpStatusCode)statusCode));

        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task Read_pipeline_retries_network_failures()
    {
        var attempts = await CountAttempts(
            NetSuitePipelines.InteractiveRead,
            new NetSuiteTransientException("network", new HttpRequestException()));

        Assert.Equal(2, attempts);
    }

    [Fact]
    public void For_selects_the_write_pipeline_for_writes()
    {
        Assert.Same(
            NetSuitePipelines.InteractiveWrite,
            NetSuitePipelines.For(Application.DataTransferObjects.Others.NS.NetSuiteRetryProfile.Interactive,
                Application.DataTransferObjects.Others.NS.NetSuiteRequestKind.Write));
    }

    [Fact]
    public void For_selects_the_read_pipeline_for_reads()
    {
        Assert.Same(
            NetSuitePipelines.InteractiveRead,
            NetSuitePipelines.For(Application.DataTransferObjects.Others.NS.NetSuiteRetryProfile.Interactive,
                Application.DataTransferObjects.Others.NS.NetSuiteRequestKind.Read));
    }
}
