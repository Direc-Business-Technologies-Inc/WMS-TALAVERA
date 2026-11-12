using System.Net;
using System.Text.Json;
using Application.DataTransferObjects.Transactions.SupplierReturn;
using Application.UseCases.Commands.Transaction.SupplierReturn;
using Integration.NS.DataTransferObjects.SupplierReturn;
using Integration.NS.Exceptions;
using Integration.NS.Implementations.Transactions;
using Integration.NS.Services;
using Integration.NS.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Entities;
using Xunit;

namespace Integration.NS.Tests.Implementations.Transactions;

public class BulkFulfillmentTests
{
    [Fact]
    public async Task Status_grid_queries_custom_records_only_on_explicit_load()
    {
        var (sut, client) = Create();
        client.QueryItems.Add(new BulkFulfillmentStatusDTO { Id = 42, Status = "Pending" });
        Assert.Empty(client.Queries);
        var (rows, count) = await sut.GetBulkFulfillmentStatusesAsync(new DataGridIntent { Take = 10 }, [7]);
        Assert.Single(rows);
        Assert.Equal(1, count);
        Assert.Contains(client.Queries, sql => sql.Contains("customrecord_bulk_fulfillment bf") &&
            sql.Contains("custrecord_bf_status") && sql.Contains("custrecord_bf_memo"));
    }

    [Fact]
    public async Task Read_only_details_resolve_suitesql_ids_and_enforce_subsidiary_scope()
    {
        var (sut, client) = Create();
        client.QueryItems.Add(new BulkFulfillmentReturnIds { ReturnIds = "341069, 341070, 341069" });
        client.QueryItems.Add(new SupplierReturnDataGridDTO { Id = 341069 });
        client.QueryItems.Add(new SupplierReturnDataGridDTO { Id = 341070 });
        var rows = await sut.GetBulkFulfillmentReturnsAsync(301, [7]);
        Assert.Equal(2, rows.Count);
        Assert.Contains("custrecord_bf_return_to_supplier_ids", client.Queries[0]);
        Assert.Contains("bf.id = 301", client.Queries[0]);
        Assert.Contains("t.id IN", client.Queries[1]);
        Assert.Contains("341069", client.Queries[1]);
        Assert.Contains("341070", client.Queries[1]);
        Assert.Contains("t.subsidiary IN", client.Queries[1]);
        Assert.Empty(client.Reads);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Missing_rts_references_do_not_query_transactions(string? references)
    {
        var (sut, client) = Create();
        client.QueryItems.Add(new BulkFulfillmentReturnIds { ReturnIds = references });
        Assert.Empty(await sut.GetBulkFulfillmentReturnsAsync(301, [7]));
        Assert.Single(client.Queries);
    }

    [Theory]
    [InlineData("341069,garbage")]
    [InlineData("0")]
    public async Task Invalid_rts_references_are_not_silently_treated_as_empty(string references)
    {
        var (sut, client) = Create();
        client.QueryItems.Add(new BulkFulfillmentReturnIds { ReturnIds = references });
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetBulkFulfillmentReturnsAsync(301, [7]));
        Assert.Single(client.Queries);
    }

    private static (SupplierReturnIntegration Sut, RestCapturingNetSuiteClient Client) Create()
    {
        var client = new RestCapturingNetSuiteClient { CreationLocation = "/record/v1/customrecord_bulk_fulfillment/42" };
        client.QueryItems.Add(new SupplierReturnNSDTO { Id = 1234 });
        client.QueryItems.Add(new SupplierReturnNSDTO { Id = 5678 });
        return (new(client, new HttpContextAccessor(), new SuiteQLQueryBuilderFactoryService()), client);
    }

    [Fact]
    public async Task Submission_creates_one_request_with_exact_unique_ids_server_date_and_submitted_status()
    {
        var (sut, client) = Create();
        var before = DateTime.Today;
        var result = await new SubmitBulkFulfillmentCmdHandler(sut).Handle(
            new(new([1234, 5678, 1234], "Warehouse RTS batch"), [7]), default);
        Assert.Equal(BulkFulfillmentOutcome.Submitted, result.Outcome);
        Assert.Equal(42, result.ReferenceId);
        var write = Assert.Single(client.Writes);
        Assert.Equal(HttpMethod.Post, write.Method);
        Assert.Equal("/record/v1/customrecord_bulk_fulfillment", write.Url);
        using var json = JsonDocument.Parse(write.Body!);
        Assert.Equal(4, json.RootElement.EnumerateObject().Count());
        Assert.Equal("1", json.RootElement.GetProperty("custrecord_bf_status").GetProperty("id").GetString());
        Assert.Equal("Warehouse RTS batch", json.RootElement.GetProperty("custrecord_bf_memo").GetString());
        Assert.Equal(new[] { "1234", "5678" }, json.RootElement.GetProperty("custrecord_bf_return_to_supplier_ids").GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetString()));
        Assert.Contains(json.RootElement.GetProperty("custrecord_bf_date").GetString(),
            new[] { before.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), DateTime.Today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) });
        Assert.Contains("vendorreturnauthorization", Assert.Single(client.Queries));
        Assert.Contains("t.subsidiary IN", client.Queries[0]);
    }

    [Fact]
    public void Payload_uses_rest_date_and_reference_shapes_for_the_reported_rejected_selection()
    {
        var payload = new BulkFulfillmentPayloadDTO(new DateOnly(2026, 11, 9),
            [338717, 338293, 338713, 335754, 335753, 335851], "November supplier return batch");
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(payload));
        Assert.Equal("2026-11-09", json.RootElement.GetProperty("custrecord_bf_date").GetString());
        Assert.Equal("November supplier return batch", json.RootElement.GetProperty("custrecord_bf_memo").GetString());
        var status = json.RootElement.GetProperty("custrecord_bf_status");
        Assert.Single(status.EnumerateObject());
        Assert.Equal("1", status.GetProperty("id").GetString());
        var references = json.RootElement.GetProperty("custrecord_bf_return_to_supplier_ids");
        Assert.Single(references.EnumerateObject());
        Assert.Equal(new[] { "338717", "338293", "338713", "335754", "335753", "335851" },
            references.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetString()));
        Assert.False(json.RootElement.TryGetProperty("custrecord_bf_error_message", out _));
    }

    [Theory]
    [InlineData(new[] { 1234, 1234 })]
    [InlineData(new[] { 0, 5678 })]
    [InlineData(new[] { -1, 1234 })]
    public async Task Invalid_selection_never_queries_or_posts(int[] ids)
    {
        var (sut, client) = Create();
        var result = await new SubmitBulkFulfillmentCmdHandler(sut).Handle(new(new(ids), [7]), default);
        Assert.Equal(BulkFulfillmentOutcome.Rejected, result.Outcome);
        Assert.Empty(client.Writes);
        Assert.Empty(client.Queries);
    }

    [Fact]
    public async Task Missing_scope_or_inaccessible_record_never_posts()
    {
        var (sut, client) = Create();
        Assert.Equal(BulkFulfillmentOutcome.Rejected, (await sut.SubmitBulkFulfillmentAsync(new([1234, 5678]), [])).Outcome);
        client.QueryItems.RemoveAt(1);
        Assert.Equal(BulkFulfillmentOutcome.Rejected, (await sut.SubmitBulkFulfillmentAsync(new([1234, 5678]), [7])).Outcome);
        Assert.Empty(client.Writes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("/record/v1/customrecord_bulk_fulfillment/not-an-id")]
    public async Task Accepted_without_reference_is_not_rejected_or_retried(string? location)
    {
        var (sut, client) = Create();
        client.CreationLocation = location;
        var result = await sut.SubmitBulkFulfillmentAsync(new([1234, 5678]), [7]);
        Assert.Equal(BulkFulfillmentOutcome.Submitted, result.Outcome);
        Assert.Null(result.ReferenceId);
        Assert.Single(client.Writes);
    }

    [Theory]
    [InlineData(400, BulkFulfillmentOutcome.Rejected)]
    [InlineData(429, BulkFulfillmentOutcome.Rejected)]
    [InlineData(500, BulkFulfillmentOutcome.Uncertain)]
    [InlineData(0, BulkFulfillmentOutcome.Uncertain)]
    public async Task Failure_outcomes_are_truthful_and_do_not_repeat_creation(int status, BulkFulfillmentOutcome expected)
    {
        var (sut, client) = Create();
        client.OnWrite = _ => throw (status == 400 ? new NetSuiteValidationException("Invalid field")
            : (Exception)new NetSuiteTransientException("Failed", status == 0 ? null : (HttpStatusCode)status));
        var result = await sut.SubmitBulkFulfillmentAsync(new([1234, 5678]), [7]);
        Assert.Equal(expected, result.Outcome);
        Assert.Single(client.Writes);
    }

    [Theory]
    [InlineData("https://example.test/record/v1/customrecord_bulk_fulfillment/42", 42)]
    [InlineData("/record/v1/customrecord_bulk_fulfillment/42?x=1", 42)]
    [InlineData("/record/v1/other/42", null)]
    [InlineData("/record/v1/customrecord_bulk_fulfillment/-1", null)]
    public void Receipt_parsing_checks_record_type_and_positive_id(string location, int? expected) =>
        Assert.Equal(expected, SupplierReturnIntegration.ParseBulkFulfillmentReference(location));
}
