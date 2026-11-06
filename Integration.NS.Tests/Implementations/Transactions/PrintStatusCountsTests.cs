using Application.DataTransferObjects.Transactions;
using Integration.NS.Implementations.Transactions;
using Integration.NS.Implementations.Transactions.Packing;
using Integration.NS.Implementations.Others.Caching;
using Integration.NS.Services;
using Integration.NS.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Entities;
using Shared.Libraries.Utilities;
using System.Security.Claims;
using System.Text.Json;
using Xunit;

namespace Integration.NS.Tests.Implementations.Transactions;

public class PrintStatusCountsTests
{
    private static HttpContextAccessor Context() => new()
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("com.direcbusiness.wms.nsAllowedLocations", "[11,12]"),
                new Claim("com.direcbusiness.wms.nsAllowedSubsidiaries", "[7]"),
                new Claim("com.direcbusiness.wms.nsEmployeeId", "99")
            }, "test"))
        }
    };

    private static ReceivingIntegration Receiving(CapturingNetSuiteClient client) => new(
        client, Context(), new SuiteQLQueryBuilderFactoryService(), NullLogger<ReceivingIntegration>.Instance);

    private static ReturnPackingIntegration Packing(CapturingNetSuiteClient client) => new(
        client, Context(), null!, new SuiteQLQueryBuilderFactoryService()); // SQL detail reader is not used by list/count queries.

    private static DataGridIntent Intent(string source)
    {
        var intent = new DataGridIntent { Take = 20, Skip = 40 };
        intent.Filters.Add(DataGridFilterUtilities.Equal("tcf.recordtype", source));
        intent.Filters.Add(DataGridFilterUtilities.GreaterThanOrEqual("tcf.trandate", new DateTime(2026, 10, 1)));
        intent.Sorts.Add(DataGridSortUtilities.Descending("Date"));
        return intent;
    }

    private static string Eligibility(string query)
    {
        var from = query.IndexOf(" FROM transaction t ", StringComparison.Ordinal);
        Assert.True(from >= 0);
        var order = query.IndexOf(" ORDER BY ", from, StringComparison.Ordinal);
        return order < 0 ? query[from..] : query[from..order];
    }

    private static void AssertAggregate(CapturingNetSuiteClient client)
    {
        var query = Assert.IsType<string>(client.LastQuery);
        Assert.StartsWith("SELECT COUNT(DISTINCT CASE WHEN t.custbody_wms_print_count IS NULL OR t.custbody_wms_print_count <= 0 THEN t.id END) AS ForPrinting", query);
        Assert.Contains("COUNT(DISTINCT CASE WHEN t.custbody_wms_print_count >= 1 THEN t.id END) AS Printed", query);
        Assert.DoesNotContain("ORDER BY", query);
        Assert.DoesNotContain("TO_CHAR", query);
        Assert.Equal(1, client.LastLimit);
        Assert.Equal(0, client.LastOffset);
    }

    [Theory]
    [InlineData("transferorder")]
    [InlineData("intercompanytransferorder")]
    [InlineData("vendorreturnauthorization")]
    public async Task Packing_counts_reuse_list_eligibility_in_one_request(string source)
    {
        var counts = new PrintStatusCountsDTO { ForPrinting = 8, Printed = 3 };
        var client = new CapturingNetSuiteClient { NextResult = counts };
        var sut = Packing(client);
        await sut.GetPackedItemFulfillments(Intent(source));
        var listQuery = client.LastQuery!;
        using var cancellation = new CancellationTokenSource();

        var result = await sut.GetPackingPrintStatusCounts(Intent(source), cancellation.Token);

        Assert.Same(counts, result);
        Assert.Equal(2, client.CallCount); // one list + one aggregate, not two status reads
        AssertAggregate(client);
        Assert.Equal(Eligibility(listQuery), Eligibility(client.LastQuery!));
        Assert.Contains("t.custbody_dbti_print_label = 'T'", client.LastQuery);
        Assert.Contains("t.subsidiary IN (7)", client.LastQuery);
        Assert.Equal(cancellation.Token, client.LastCancellationToken);
    }

    [Theory]
    [InlineData("purchaseorder")]
    [InlineData("transferorder")]
    [InlineData("intercompanytransferorder")]
    public async Task Receiving_counts_preserve_scope_date_and_status_without_source_line_fanout(string source)
    {
        var counts = new PrintStatusCountsDTO { ForPrinting = 4, Printed = 2 };
        var client = new CapturingNetSuiteClient { NextResult = counts };
        var sut = Receiving(client);
        var listIntent = Intent(source);
        listIntent.Filters.Add(DataGridFilterUtilities.Equal("Status", "Pending Receipt"));
        await sut.GetItemReceiptsDatagrid(listIntent);
        var listQuery = client.LastQuery!;
        var countIntent = Intent(source);
        countIntent.Filters.Add(DataGridFilterUtilities.Equal("Status", "Pending Receipt"));

        var result = await sut.GetReceivingPrintStatusCounts(countIntent);

        Assert.Same(counts, result);
        Assert.Equal(2, client.CallCount);
        AssertAggregate(client);
        Assert.Equal(Eligibility(listQuery), Eligibility(client.LastQuery!));
        Assert.Contains("SELECT DISTINCT pol.transaction AS sourceid", client.LastQuery);
        Assert.Contains("pol.location IN (11,12)", client.LastQuery);
        Assert.Contains("allowedpo.sourceid IS NOT NULL", client.LastQuery);
        Assert.Contains("BUILTIN.DF(tcf.status) = 'Pending Receipt'", client.LastQuery);
        Assert.DoesNotContain("transactionline ptl", client.LastQuery);
    }

    [Fact]
    public async Task Purchase_order_location_gate_joins_unique_eligible_source_ids()
    {
        var client = new CapturingNetSuiteClient();
        await Receiving(client).GetPurchaseOrdersListAsync(new DataGridIntent());

        Assert.Contains("JOIN (SELECT DISTINCT pol.transaction AS sourceid", client.LastQuery);
        Assert.Contains("po.recordtype = 'purchaseorder'", client.LastQuery);
        Assert.Contains("pol.location IN (11,12)", client.LastQuery);
        Assert.Contains("allowedpo.sourceid = t.id", client.LastQuery);
        Assert.DoesNotContain("JOIN transactionline tl", client.LastQuery);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Cancelled_badge_queries_do_not_execute_transport_work(bool packing)
    {
        var client = new CapturingNetSuiteClient();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => packing
            ? Packing(client).GetPackingPrintStatusCounts(Intent("transferorder"), cancellation.Token)
            : Receiving(client).GetReceivingPrintStatusCounts(Intent("purchaseorder"), cancellation.Token));

        Assert.Equal(cancellation.Token, client.LastCancellationToken);
        Assert.Equal(0, client.CallCount);
    }

    [Fact]
    public void NetSuite_lowercase_aggregate_aliases_deserialize()
    {
        var counts = JsonSerializer.Deserialize<PrintStatusCountsDTO>("{\"forprinting\":0,\"printed\":12}");
        Assert.NotNull(counts);
        Assert.Equal(0, counts.ForPrinting);
        Assert.Equal(12, counts.Printed);
    }

    [Fact]
    public async Task Receiving_badge_cache_distinguishes_filters_and_honors_receiving_invalidation()
    {
        using var store = new GridCacheStore(100, TimeSpan.FromSeconds(60));
        var cache = new GridResultCache(store, Context(), null);
        var client = new CapturingNetSuiteClient { NextResult = new PrintStatusCountsDTO { Printed = 2 } };
        var sut = new CachedReceivingIntegration(Receiving(client), cache);

        await sut.GetReceivingPrintStatusCounts(Intent("purchaseorder"));
        await sut.GetReceivingPrintStatusCounts(Intent("purchaseorder"));
        Assert.Equal(1, client.CallCount);

        await sut.GetReceivingPrintStatusCounts(Intent("transferorder"));
        Assert.Equal(2, client.CallCount);

        cache.Invalidate(CachedReceivingIntegration.EntityName);
        client.NextResult = new PrintStatusCountsDTO { Printed = 3 };
        var refreshed = await sut.GetReceivingPrintStatusCounts(Intent("purchaseorder"));
        Assert.Equal(3, refreshed.Printed);
        Assert.Equal(3, client.CallCount);
    }
}
