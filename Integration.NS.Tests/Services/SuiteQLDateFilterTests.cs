using Integration.NS.Services;
using Shared.Entities;
using Shared.Libraries.Utilities;
using Xunit;
using Integration.NS.Implementations.Transactions.Packing;
using Integration.NS.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Integration.NS.Tests.Services;

public class SuiteQLDateFilterTests
{
    [Theory]
    [InlineData("STR")]
    [InlineData("RETURNS")]
    [InlineData("VRA")]
    public async Task Packing_source_order_range_filters_native_trandate_for_every_source(string source)
    {
        var client = new CapturingNetSuiteClient();
        var context = new HttpContextAccessor();
        var factory = new SuiteQLQueryBuilderFactoryService();
        var intent = new DataGridIntent { Take = 10 };
        intent.Filters.Add(DataGridFilterUtilities.GreaterThanOrEqual("Date", new DateTime(2026, 10, 1)));
        intent.Filters.Add(DataGridFilterUtilities.LessThan("Date", new DateTime(2026, 10, 6)));
        switch (source)
        {
            case "STR":
                await new StockTransferRequestPackingIntegration(client, context, null!, factory,
                    NullLogger<StockTransferRequestPackingIntegration>.Instance).GetPackingStockTransferRequestList(intent, 7);
                break;
            case "RETURNS":
                await new ReturnPackingIntegration(client, context, null!, factory).GetPackingReturnsList(intent, 7);
                break;
            default:
                await new VendorReturnAuthorizationPackingIntegration(client, null!, context, factory).GetPackingVendorReturnAuthorizationsList(intent, 7);
                break;
        }
        var query = Assert.IsType<string>(client.LastQuery);
        var where = query[query.IndexOf(" WHERE ", StringComparison.Ordinal)..];
        Assert.Contains("t.trandate >= TO_DATE('2026-10-01T00:00:00'", where);
        Assert.Contains("t.trandate < TO_DATE('2026-10-06T00:00:00'", where);
        Assert.DoesNotContain("TO_CHAR(t.trandate", where);
        Assert.Single(client.Calls);
    }

    [Fact]
    public void Date_range_compares_native_date_column_to_converted_literals()
    {
        var intent = new DataGridIntent { Take = 10 };
        intent.Filters.Add(DataGridFilterUtilities.GreaterThanOrEqual("Date", new DateTime(2026, 10, 28)));
        intent.Filters.Add(DataGridFilterUtilities.LessThan("Date", new DateTime(2026, 10, 30)));

        var query = new SuiteQLQueryBuilderFactoryService().Create()
            .Select(("TO_CHAR(t.trandate, 'YYYY-MM-DD\"T\"HH24:MI:SS')", "Date"))
            .From("transaction t")
            .WithPropertyMap("Date", "t.trandate")
            .WithDatagridIntent(intent)
            .Build().Query;

        Assert.Contains("t.trandate >= TO_DATE('2026-10-28T00:00:00', 'YYYY-MM-DD\"T\"HH24:MI:SS')", query);
        Assert.Contains("t.trandate < TO_DATE('2026-10-30T00:00:00', 'YYYY-MM-DD\"T\"HH24:MI:SS')", query);
        Assert.DoesNotContain("TO_DATE(t.trandate", query);
        Assert.DoesNotContain("WHERE TO_CHAR(t.trandate", query);
    }

    [Fact]
    public void Custom_record_date_column_uses_native_date_for_filtering()
    {
        var intent = new DataGridIntent { Take = 10 };
        intent.Filters.Add(DataGridFilterUtilities.GreaterThanOrEqual("TripDate", new DateTime(2026, 10, 29)));

        var query = new SuiteQLQueryBuilderFactoryService().Create()
            .Select(("TO_CHAR(tt.custrecord_dbti_trt_date, 'YYYY-MM-DD\"T\"HH24:MI:SS')", "TripDate"))
            .From("customrecord_dbti_trip_ticket tt")
            .WithPropertyMap("TripDate", "tt.custrecord_dbti_trt_date")
            .WithDatagridIntent(intent)
            .Build().Query;

        Assert.Contains("tt.custrecord_dbti_trt_date >= TO_DATE('2026-10-29T00:00:00'", query);
        Assert.DoesNotContain("TO_DATE(tt.custrecord_dbti_trt_date", query);
    }
}
