using Application.DataTransferObjects.Transactions.Receiving;
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
}
