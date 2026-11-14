using Xunit;

namespace Integration.NS.Tests.Implementations.Transactions;

// Preserve the 2026-10-13 unsupported-correlated-SuiteQL regression while
// asserting the 2026-10-27 operational identity contract instead of IF accounting ids.
public sealed class GetItemReceiptItemFulfillmentLinesQueryShapeTests
{
    [Fact]
    public async Task Fulfillment_lookup_is_scoped_by_tranid_and_record_type()
    {
        var fixture = new ReceivingOperationalLinesTests.Fixture();
        await fixture.Sut.GetItemReceiptItemFulfillmentLinesAsync("IF230512");
        Assert.Contains("t.tranid = 'IF230512'", fixture.Client.Queries[0]);
        Assert.Contains("t.recordtype = 'itemfulfillment'", fixture.Client.Queries[0]);
    }

    [Fact]
    public async Task Mapping_uses_native_operational_nextline_and_source_pair_identity()
    {
        var fixture = new ReceivingOperationalLinesTests.Fixture();
        await fixture.Sut.GetItemReceiptItemFulfillmentLinesAsync("IF230512");
        var query = Assert.Single(fixture.Client.Queries.Where(query => query.Contains("AS ShippingLine")));
        Assert.Contains("link.nextline AS OperationalLine", query);
        Assert.Contains("link.nextdoc = 456", query);
        Assert.Contains("link.previousdoc = 208281", query);
        Assert.Contains("link.linktype = 'ShipRcpt'", query);
        Assert.Contains("shipping.transactionlinetype = 'SHIPPING'", query);
        Assert.Contains("receiving.transactionlinetype = 'RECEIVING'", query);
        Assert.DoesNotContain("transactionline ifline", query);
        Assert.DoesNotContain("unitstypeuom", query);
    }

    [Fact]
    public async Task Receipt_allocation_is_scoped_to_selected_IF_without_join_fanout_or_quantity_distinct()
    {
        var fixture = new ReceivingOperationalLinesTests.Fixture();
        fixture.Receipts.Add(701, ReceivingOperationalLinesTests.Fixture.Record(701, 300, 1));
        await fixture.Sut.GetItemReceiptItemFulfillmentLinesAsync("IF230512");
        Assert.Contains(fixture.Client.Queries, query => query.Contains("FROM nexttransactionlinelink link") &&
            query.Contains("link.previousdoc = 456") && query.Contains("link.linktype = 'TOrdCost'"));
        Assert.Contains(fixture.Client.Queries, query => query.Contains("link.nextdoc = 701") &&
            query.Contains("link.nexttype = 'ItemRcpt'") && query.Contains("link.linktype = 'ShipRcpt'"));
        Assert.All(fixture.Client.Queries, query => Assert.DoesNotContain("SUM(", query));
    }

    [Fact]
    public async Task Select_lists_remain_free_of_correlated_scalar_subqueries()
    {
        var fixture = new ReceivingOperationalLinesTests.Fixture();
        await fixture.Sut.GetItemReceiptItemFulfillmentLinesAsync("IF230512");
        foreach (var query in fixture.Client.Queries)
        {
            var from = query.IndexOf("FROM", StringComparison.OrdinalIgnoreCase);
            Assert.True(from > 0);
            Assert.DoesNotContain("(SELECT", query[..from]);
        }
    }
}
