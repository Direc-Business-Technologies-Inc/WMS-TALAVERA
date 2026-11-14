using Integration.NS.Implementations.Transactions;
using Integration.NS.Services;
using Integration.NS.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Integration.NS.Tests.Implementations.Transactions;

public class GetItemReceiptLinesQueryShapeTests
{
    [Fact]
    public async Task Receipt_line_query_aggregates_one_to_many_lookups_before_joining()
    {
        var client = new CapturingNetSuiteClient();
        var sut = new ReceivingIntegration(
            client,
            new HttpContextAccessor(),
            new SuiteQLQueryBuilderFactoryService(),
            NullLogger<ReceivingIntegration>.Instance);

        await sut.GetItemReceiptLinesAsync("ISTR-1", transferorder: true);

        var query = Assert.IsType<string>(client.LastQuery);
        Assert.Contains("COUNT(DISTINCT ibq.bin)", query);
        Assert.Contains("GROUP BY ibq.item, b.location", query);
        Assert.Contains("MIN(b.name) AS name", query);
        Assert.Contains("GROUP BY b.custrecord_bpu_item, b.custrecord_bpu_uom", query);
        Assert.Contains("tl.transactionlinetype = 'RECEIVING'", query);
    }
}
