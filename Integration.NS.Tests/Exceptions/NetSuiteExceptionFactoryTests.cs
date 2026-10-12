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

    [Fact]
    public void Offset_out_of_bounds_message_is_classified_for_retry()
    {
        // Exact wording from the reported failure: detail carries the hint,
        // message carries the INVALID_PARAMETER envelope.
        var exception = Assert.IsType<NetSuiteValidationException>(
            NetSuiteExceptionFactory.CreateForStatus(
                HttpStatusCode.NotFound,
                "NetSuite 404 Not Found: Not Found(404): [INVALID_PARAMETER] The specified query parameter 'offset' is out of bounds. Provide value between 0 and 20",
                "The specified query parameter 'offset' is out of bounds. Provide value between 0 and 20"));

        Assert.Equal(NetSuiteErrorCode.OffsetOutOfBounds, exception.ErrorCode);
    }

    [Fact]
    public void Offset_out_of_bounds_is_classified_from_message_when_detail_is_null()
    {
        // One SuiteQL path passes a null detail; the classification must still
        // fire from the message text.
        var exception = Assert.IsType<NetSuiteValidationException>(
            NetSuiteExceptionFactory.CreateForStatus(
                HttpStatusCode.NotFound,
                "NetSuite 404 Not Found: offset is out of bounds. Provide value between 0 and 20",
                null));

        Assert.Equal(NetSuiteErrorCode.OffsetOutOfBounds, exception.ErrorCode);
    }

    [Theory]
    [InlineData("The specified query parameter 'offset' is out of bounds. Provide value between 0 and 20", 20)]
    [InlineData("offset must be divisible by the page limit", null)]
    [InlineData("Insufficient stock", null)]
    [InlineData(null, null)]
    public void Offset_upper_bound_is_parsed_from_the_between_hint(string? text, int? expected)
    {
        Assert.Equal(expected, NetSuiteErrorCodes.GetOffsetUpperBound("NetSuite 404 wrapper", text));
    }

    [Fact]
    public void Unrelated_validation_failure_stays_unknown()
    {
        var exception = Assert.IsType<NetSuiteValidationException>(
            NetSuiteExceptionFactory.CreateForStatus(
                HttpStatusCode.BadRequest,
                "NetSuite 400 Bad Request: Insufficient stock",
                "Insufficient stock"));

        Assert.Equal(NetSuiteErrorCode.Unknown, exception.ErrorCode);
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
