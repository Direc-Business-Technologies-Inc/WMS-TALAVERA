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

    [Fact]
    public void Invalid_or_unsupported_search_is_classified()
    {
        // Exact wording from the reported STR item-receipt failure.
        var exception = Assert.IsType<NetSuiteValidationException>(
            NetSuiteExceptionFactory.CreateForStatus(
                HttpStatusCode.BadRequest,
                "NetSuite 400 Bad Request: Bad Request(400): [INVALID_PARAMETER] Invalid search query. Detailed unprocessed description follows. Search error occured: Invalid or unsupported search.",
                "Invalid search query. Detailed unprocessed description follows. Search error occured: Invalid or unsupported search."));

        Assert.Equal(NetSuiteErrorCode.UnsupportedSearch, exception.ErrorCode);
    }

    [Fact]
    public void Unsupported_search_appends_the_suiteql_excerpt_from_the_request_body()
    {
        const string body = """{"q":"SELECT item.itemid AS ItemCode FROM transactionline tl JOIN item ON tl.item = item.id WHERE t.tranid = 'IF-1'"}""";

        var exception = Assert.IsType<NetSuiteValidationException>(
            NetSuiteExceptionFactory.CreateForStatus(
                HttpStatusCode.BadRequest,
                "NetSuite 400 Bad Request: Invalid or unsupported search.",
                "Invalid or unsupported search.",
                body));

        Assert.Contains(
            "Failing query (excerpt): SELECT item.itemid AS ItemCode FROM transactionline tl JOIN item ON tl.item = item.id WHERE t.tranid = 'IF-1'",
            exception.Message);
    }

    [Fact]
    public void Unsupported_search_excerpt_is_truncated_at_500_characters()
    {
        var longQuery = new string('x', 600);
        var body = System.Text.Json.JsonSerializer.Serialize(new { q = longQuery });

        var exception = Assert.IsType<NetSuiteValidationException>(
            NetSuiteExceptionFactory.CreateForStatus(
                HttpStatusCode.BadRequest,
                "NetSuite 400 Bad Request: Invalid or unsupported search.",
                "Invalid or unsupported search.",
                body));

        var excerpt = exception.Message.Split("Failing query (excerpt): ")[1];
        Assert.Equal(NetSuiteExceptionFactory.QueryExcerptLimit + " …(truncated)".Length, excerpt.Length);
    }

    [Fact]
    public void Unsupported_search_without_a_request_body_keeps_the_message_unchanged()
    {
        var exception = Assert.IsType<NetSuiteValidationException>(
            NetSuiteExceptionFactory.CreateForStatus(
                HttpStatusCode.BadRequest,
                "NetSuite 400 Bad Request: Invalid or unsupported search.",
                "Invalid or unsupported search."));

        Assert.Equal("NetSuite 400 Bad Request: Invalid or unsupported search.", exception.Message);
    }

    [Fact]
    public void Other_validation_failures_ignore_the_request_body()
    {
        var exception = Assert.IsType<NetSuiteValidationException>(
            NetSuiteExceptionFactory.CreateForStatus(
                HttpStatusCode.BadRequest,
                "NetSuite 400 Bad Request: Insufficient stock",
                "Insufficient stock",
                """{"q":"SELECT 1"}"""));

        Assert.Equal("NetSuite 400 Bad Request: Insufficient stock", exception.Message);
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
