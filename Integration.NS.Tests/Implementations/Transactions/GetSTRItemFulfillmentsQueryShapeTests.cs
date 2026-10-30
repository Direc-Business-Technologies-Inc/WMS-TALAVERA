using Application.DataTransferObjects.Transactions.Receiving;
using Application.DataTransferObjects.Others.NS;
using Integration.NS.Implementations.Transactions;
using Shared.Entities;
using Xunit;

namespace Integration.NS.Tests.Implementations.Transactions;

public sealed class GetSTRItemFulfillmentsQueryShapeTests
{
    [Theory]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public async Task Picker_uses_verified_operational_remainders_before_paging(decimal received, bool pending)
    {
        var fixture = new ReceivingOperationalLinesTests.Fixture();
        fixture.Client.QueryItems.Add(new ItemFulfillmentDTO { Id = 456, ReferenceNumber = "IF230512" });
        fixture.Receipts.Add(701, ReceivingOperationalLinesTests.Fixture.Record(701, 300, received));

        var result = await fixture.Sut.GetSTRItemFulfillments(208281, new DataGridIntent());

        Assert.Equal(pending, result.Items.Any());
        Assert.Contains("ml.createdfrom = 208281", fixture.Client.Queries[0]);
        Assert.All(fixture.Client.Queries, query =>
        {
            Assert.DoesNotContain("SUM(ABS(", query);
            Assert.DoesNotContain("custbody_dbti_fully_received", query);
        });
        if (pending) Assert.Contains(fixture.Client.Queries, query => query.Contains("t.id IN (456)"));
    }

    [Fact]
    public async Task Unverifiable_candidate_does_not_hide_other_receivable_fulfillments()
    {
        var fixture = new ReceivingOperationalLinesTests.Fixture();
        var invalidCandidate = ReceivingOperationalLinesTests.Fixture.Record(253092, 299, 2);
        invalidCandidate["item"]!["items"]![0]!.AsObject().Remove("units");
        fixture.Client.OnRead = url => System.Text.Json.JsonSerializer.SerializeToElement(
            url.Contains("/253092?") ? invalidCandidate : fixture.Fulfillment);
        fixture.Client.QueryItems.Add(new ItemFulfillmentDTO { Id = 253092, ReferenceNumber = "IF-INVALID" });
        var validCandidate = new ItemFulfillmentDTO { Id = 456, ReferenceNumber = "IF-VALID" };
        fixture.Client.QueryItems.Add(validCandidate);
        var originalOnQuery = fixture.Client.OnQuery!;
        fixture.Client.OnQuery = (query, type) => type == typeof(ItemFulfillmentDTO) && query.Contains("t.id IN (456)")
            ? new Application.DataTransferObjects.Others.NS.NetSuiteResponse<ItemFulfillmentDTO>
            {
                items = [validCandidate], totalResults = 1
            }
            : originalOnQuery(query, type);

        var result = await fixture.Sut.GetSTRItemFulfillments(208281, new DataGridIntent());

        Assert.Equal(1, result.TotalResults);
        Assert.Equal(1, result.SkippedUnverifiableCount);
        Assert.Equal(456, Assert.Single(result.Items).Id);
        Assert.Contains(fixture.Client.Queries, query => query.Contains("t.id IN (456)"));
    }

    [Fact]
    public async Task Picker_batches_native_receipt_links_but_selection_reads_them_fresh()
    {
        var fixture = new ReceivingOperationalLinesTests.Fixture();
        fixture.Client.QueryItems.Add(new ItemFulfillmentDTO { Id = 456, ReferenceNumber = "IF230512" });
        fixture.Receipts.Add(701, ReceivingOperationalLinesTests.Fixture.Record(701, 300, 2));

        await fixture.Sut.GetSTRItemFulfillments(208281, new DataGridIntent());

        Assert.Single(fixture.Client.Queries, query => query.Contains("link.previousdoc IN (456)") &&
            query.Contains("nexttransactionlinelink"));
        Assert.Single(fixture.Client.Queries, query => query.Contains("link.nextdoc IN (701)") &&
            query.Contains("nexttransactionlinelink"));
        Assert.DoesNotContain(fixture.Client.Queries, query => query.Contains("link.previousdoc = 456") &&
            query.Contains("nexttransactionlinelink"));
        Assert.DoesNotContain(fixture.Client.Queries, query => query.Contains("link.nextdoc = 701") &&
            query.Contains("nexttransactionlinelink"));

        await fixture.Sut.GetItemReceiptItemFulfillmentLinesAsync("IF230512");
        Assert.Contains(fixture.Client.Queries, query => query.Contains("link.previousdoc = 456") &&
            query.Contains("nexttransactionlinelink"));
        Assert.Contains(fixture.Client.Queries, query => query.Contains("link.nextdoc = 701") &&
            query.Contains("nexttransactionlinelink"));
    }

    [Fact]
    public async Task Incomplete_batch_uses_fresh_per_fulfillment_links_instead()
    {
        var fixture = new ReceivingOperationalLinesTests.Fixture();
        fixture.Client.QueryItems.Add(new ItemFulfillmentDTO { Id = 456, ReferenceNumber = "IF230512" });
        fixture.Receipts.Add(701, ReceivingOperationalLinesTests.Fixture.Record(701, 300, 2));
        var original = fixture.Client.OnQuery!;
        fixture.Client.OnQuery = (query, type) => type == typeof(ReceivingIntegration.OperationalReceiptLink) &&
            query.Contains("link.previousdoc IN (456)")
            ? new NetSuiteResponse<ReceivingIntegration.OperationalReceiptLink>
                { items = [], hasMore = true, totalResults = 1001 }
            : original(query, type);

        var result = await fixture.Sut.GetSTRItemFulfillments(208281, new DataGridIntent());

        Assert.Equal(0, result.SkippedUnverifiableCount);
        Assert.Single(result.Items);
        Assert.Contains(fixture.Client.Queries, query => query.Contains("link.previousdoc = 456") &&
            query.Contains("nexttransactionlinelink"));
    }

    [Fact]
    public async Task Picker_reuses_mapped_TO_metadata_once_and_preserves_independent_IF_quantities()
    {
        var fixture = new ReceivingOperationalLinesTests.Fixture();
        fixture.Client.QueryItems.Add(new ItemFulfillmentDTO { Id = 456, ReferenceNumber = "IF456" });
        fixture.Client.QueryItems.Add(new ItemFulfillmentDTO { Id = 457, ReferenceNumber = "IF457" });
        fixture.Mappings.Add(new ReceivingIntegration.OperationalTransferLink
        {
            FulfillmentId = 457, SourceId = 208281, ShippingLine = 299, OperationalLine = 0,
            ReceivingLine = 300, ItemId = 49312, UnitId = 1
        });
        fixture.Receipts.Add(701, ReceivingOperationalLinesTests.Fixture.Record(701, 300, 3));
        var second = ReceivingOperationalLinesTests.Fixture.Record(457, 299, 2);
        fixture.Client.OnRead = url => System.Text.Json.JsonSerializer.SerializeToElement(
            url.Contains("/457?") ? second : url.Contains("itemFulfillment")
                ? fixture.Fulfillment : fixture.Receipts[701]);
        var original = fixture.Client.OnQuery!;
        fixture.Client.OnQuery = (query, type) => type == typeof(ReceivingIntegration.OperationalTransferLink) &&
            query.Contains("link.nextdoc IN (456, 457)")
            ? new NetSuiteResponse<ReceivingIntegration.OperationalTransferLink>
                { items = fixture.Mappings, totalResults = fixture.Mappings.Count }
            : type == typeof(ReceivingIntegration.OperationalTransferLink) && query.Contains("link.nextdoc =")
                ? new NetSuiteResponse<ReceivingIntegration.OperationalTransferLink>
                    { items = fixture.Mappings.Where(map => query.Contains($"link.nextdoc = {map.FulfillmentId}" )).ToList(), totalResults = 1 }
                : original(query, type);

        await fixture.Sut.GetSTRItemFulfillments(208281, new DataGridIntent());

        Assert.Single(fixture.Client.Queries, query => query.Contains("link.nextdoc IN (") &&
            query.Contains("previoustransactionlinelink"));
        Assert.Single(fixture.Client.Queries, query => query.Contains("item.usebins") && query.Contains("tl.id IN (300)"));
        Assert.Single(fixture.Client.Queries, query => query.Contains("t.tranid") && query.Contains("t.id = 208281"));
        Assert.Contains(fixture.Client.Queries, query => query.Contains("t.id IN (457)") &&
            query.Contains("itemfulfillmentstatus"));
    }

    [Fact]
    public async Task Incomplete_source_batch_falls_back_to_per_IF_metadata()
    {
        var fixture = new ReceivingOperationalLinesTests.Fixture();
        fixture.Client.QueryItems.Add(new ItemFulfillmentDTO { Id = 456, ReferenceNumber = "IF456" });
        var original = fixture.Client.OnQuery!;
        fixture.Client.OnQuery = (query, type) => type == typeof(ReceivingIntegration.OperationalTransferLink) &&
            query.Contains("link.nextdoc IN (456)")
            ? new NetSuiteResponse<ReceivingIntegration.OperationalTransferLink>
                { items = [], totalResults = 1001, hasMore = true }
            : original(query, type);

        var result = await fixture.Sut.GetSTRItemFulfillments(208281, new DataGridIntent());

        Assert.Single(result.Items);
        Assert.Equal(0, result.SkippedUnverifiableCount);
        Assert.Contains(fixture.Client.Queries, query => query.Contains("link.nextdoc = 456") &&
            query.Contains("previoustransactionlinelink"));
    }
}
