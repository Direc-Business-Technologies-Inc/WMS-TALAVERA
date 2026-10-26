using System.Text.RegularExpressions;
using Integration.NS.Implementations.Transactions;
using Integration.NS.Services;
using Integration.NS.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Entities;
using Xunit;

namespace Integration.NS.Tests.Implementations.Transactions;

public sealed class GetSTRItemFulfillmentsQueryShapeTests
{
    [Fact]
    public async Task Picker_includes_only_fulfillments_with_native_linked_receipt_remainder()
    {
        var client = new CapturingNetSuiteClient();
        var sut = new ReceivingIntegration(
            client,
            new HttpContextAccessor(),
            new SuiteQLQueryBuilderFactoryService(),
            NullLogger<ReceivingIntegration>.Instance);

        await sut.GetSTRItemFulfillments(123, new DataGridIntent());

        var query = Regex.Replace(client.LastQuery!, @"\s+", " ").Trim();
        Assert.Contains("nexttransactionlink ifreceipt", query);
        Assert.Contains("ifreceipt.linktype = 'TOrdCost'", query);
        Assert.Contains("NVL(irqty.qtyreceived, 0) < ifqty.qtyfulfilled", query);
        Assert.DoesNotContain("custbody_dbti_fully_received", query);
        Assert.Contains("t.status = 'C'", query);
        Assert.Contains("ml.createdfrom = 123", query);
    }
}
