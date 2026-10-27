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

        var (items, _) = await fixture.Sut.GetSTRItemFulfillments(208281, new DataGridIntent());

        Assert.Equal(pending, items.Any());
        Assert.Contains("ml.createdfrom = 208281", fixture.Client.Queries[0]);
        Assert.All(fixture.Client.Queries, query =>
        {
            Assert.DoesNotContain("SUM(ABS(", query);
            Assert.DoesNotContain("custbody_dbti_fully_received", query);
        });
        if (pending) Assert.Contains(fixture.Client.Queries, query => query.Contains("t.id IN (456)"));
    }
}
