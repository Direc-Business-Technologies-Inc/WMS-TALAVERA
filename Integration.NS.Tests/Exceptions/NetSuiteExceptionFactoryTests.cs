using System.Net;
using Integration.NS.Exceptions;
using Xunit;

namespace Integration.NS.Tests.Exceptions;

public class NetSuiteExceptionFactoryTests
{
    [Theory]
    [InlineData(408)] // Request Timeout
    [InlineData(429)] // Too Many Requests
    [InlineData(500)]
    [InlineData(502)]
    [InlineData(503)]
    [InlineData(504)]
    public void CreateForStatus_classifies_transient_statuses_as_transient(int statusCode)
    {
        var exception = NetSuiteExceptionFactory.CreateForStatus(
            (HttpStatusCode)statusCode,
            "NetSuite message",
            "detail");

        Assert.IsType<NetSuiteTransientException>(exception);
        Assert.Equal("NetSuite message", exception.Message);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(409)]
    [InlineData(422)]
    public void CreateForStatus_classifies_other_4xx_as_validation(int statusCode)
    {
        var exception = NetSuiteExceptionFactory.CreateForStatus(
            (HttpStatusCode)statusCode,
            "NetSuite message",
            "detail");

        Assert.IsType<NetSuiteValidationException>(exception);
        Assert.Equal("NetSuite message", exception.Message);
    }

    [Fact]
    public void Validation_exception_exposes_the_parsed_detail()
    {
        var exception = Assert.IsType<NetSuiteValidationException>(
            NetSuiteExceptionFactory.CreateForStatus(
                HttpStatusCode.BadRequest,
                "NetSuite 400 Bad Request: Insufficient stock",
                "Insufficient stock"));

        Assert.Equal("Insufficient stock", exception.Detail);
    }

    [Theory]
    [InlineData(408)]
    [InlineData(429)]
    [InlineData(500)]
    [InlineData(503)]
    public void CreateForStatus_sets_the_status_code_on_transient_exceptions(int statusCode)
    {
        var exception = Assert.IsType<NetSuiteTransientException>(
            NetSuiteExceptionFactory.CreateForStatus(
                (HttpStatusCode)statusCode,
                "NetSuite message",
                "detail"));

        Assert.Equal((HttpStatusCode)statusCode, exception.StatusCode);
    }

    [Fact]
    public void CreateForNetworkError_leaves_status_code_null()
    {
        var exception = NetSuiteExceptionFactory.CreateForNetworkError(
            "NetSuite request timed out",
            new TaskCanceledException());

        Assert.Null(exception.StatusCode);
    }

    [Fact]
    public void Network_error_is_wrapped_as_transient_with_inner_exception()
    {
        var inner = new HttpRequestException("connection reset");

        var exception = NetSuiteExceptionFactory.CreateForNetworkError(
            "NetSuite request failed: connection reset",
            inner);

        Assert.IsType<NetSuiteTransientException>(exception);
        Assert.Equal("NetSuite request failed: connection reset", exception.Message);
        Assert.Same(inner, exception.InnerException);
    }

    [Theory]
    [InlineData(408, true)]
    [InlineData(429, true)]
    [InlineData(500, true)]
    [InlineData(400, false)]
    [InlineData(404, false)]
    public void IsTransientStatus_matches_the_classification(int statusCode, bool expected)
    {
        Assert.Equal(expected, NetSuiteExceptionFactory.IsTransientStatus((HttpStatusCode)statusCode));
    }
}
