using System.Security.Claims;
using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions;
using Application.DataTransferObjects.Transactions.TripTicket.NS;
using Application.UseCases.Queries.Transaction.TripTicket.NS;
using Integration.NS.Helpers;
using Integration.NS.Implementations.Transactions;
using Integration.NS.Implementations.Transactions.Packing;
using Integration.NS.Services;
using Integration.NS.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Entities;
using Shared.Libraries.Utilities;
using Xunit;

namespace Integration.NS.Tests.Implementations.Transactions;

public class TripTicketSubsidiaryScopeTests
{
    private static HttpContextAccessor Context(string? subsidiary = "7", bool authenticated = true) => new()
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                (subsidiary is null ? Array.Empty<Claim>() : [new Claim("NsSubsidiaryId", subsidiary)])
                .Concat([
                    new Claim("com.direcbusiness.wms.nsAllowedSubsidiaries", "[7,8,9]"),
                    new Claim("com.direcbusiness.wms.nsEmployeeId", "99")
                ]), authenticated ? "test" : null))
        }
    };

    private static PostTripTicketDTO Ticket(params int[] ids) => new()
    {
        FromSubsidiary = new() { NetsuiteSubsidiaryInternalId = 7 },
        ItemFulfillments = ids.Select(id => new ItemFulfillmentDTO { NetsuiteOrderInternalId = id }).ToList()
    };

    [Fact]
    public async Task Web_picker_keeps_current_source_scope_when_searching_and_paging()
    {
        var client = new CapturingNetSuiteClient();
        var intent = new DataGridIntent { Take = 20, Skip = 40 };
        intent.Filters.Add(DataGridFilterUtilities.Contains("OrderNumber", "IF"));
        await new TripTicketIntegration(client, Context(), new SuiteQLQueryBuilderFactoryService()).GetItemfulfillments(intent);

        Assert.Contains("t.subsidiary = 7", client.LastQuery);
        Assert.DoesNotContain("t.subsidiary IN", client.LastQuery);
        Assert.DoesNotContain("t.tosubsidiary IN", client.LastQuery);
        Assert.Equal(20, client.LastLimit);
        Assert.Equal(40, client.LastOffset);
    }

    [Fact]
    public async Task Label_list_and_counts_ignore_other_allowed_destination_and_prepared_by_subsidiaries()
    {
        var client = new CapturingNetSuiteClient { NextResult = new PrintStatusCountsDTO() };
        var integration = new ReturnPackingIntegration(client, Context(), null!, new SuiteQLQueryBuilderFactoryService());
        await integration.GetPackedItemFulfillments(new DataGridIntent { Take = 20 });
        await integration.GetPackingPrintStatusCounts(new DataGridIntent { Take = 20 });

        Assert.Equal(2, client.Queries.Count);
        Assert.All(client.Queries, query =>
        {
            Assert.Contains("t.subsidiary = 7", query);
            Assert.DoesNotContain("t.tosubsidiary", query);
            Assert.DoesNotContain("t.custbody_dbti_prepared_by", query);
            Assert.DoesNotContain("t.subsidiary IN", query);
        });
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("0", true)]
    [InlineData("-1", true)]
    [InlineData("invalid", true)]
    [InlineData("7", false)]
    public async Task Missing_or_invalid_identity_never_executes_unscoped_reads(string? subsidiary, bool authenticated)
    {
        var client = new CapturingNetSuiteClient();
        var context = Context(subsidiary, authenticated);
        var factory = new SuiteQLQueryBuilderFactoryService();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            new TripTicketIntegration(client, context, factory).GetItemfulfillments(new()));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            new ReturnPackingIntegration(client, context, null!, factory).GetPackedItemFulfillments(new()));
        Assert.Empty(client.Calls);
    }

    [Fact]
    public async Task Mobile_query_supplies_subsidiary_to_the_legacy_script()
    {
        var client = new CapturingNetSuiteClient();
        using var cancellation = new CancellationTokenSource();
        await new GetPackedItemFulfillmentsQryHandler(client).Handle(new(7), cancellation.Token);
        Assert.Equal("NS_ItemFulfillment_Get_Packed", client.LastScriptName);
        Assert.Equal("7", client.LastScriptParameters!["subsidiaryid"]);
        Assert.Equal(cancellation.Token, client.LastCancellationToken);
    }

    [Fact]
    public async Task Mobile_query_rejects_an_unscoped_request()
    {
        var client = new CapturingNetSuiteClient();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            new GetPackedItemFulfillmentsQryHandler(client).Handle(new(0), default));
        Assert.Null(client.LastScriptName);
    }

    [Fact]
    public async Task Guard_accepts_own_source_ifs_without_restricting_destination()
    {
        var client = new CapturingNetSuiteClient
        {
            NextItems = [new OrdersDTO { NetsuiteOrderInternalId = 101, NetsuiteFromSubsidiaryInternalId = 7, NetsuiteToSubsidiaryInternalId = 8 }]
        };
        await TripTicketSubsidiaryGuard.ValidateAsync(client, Context(), Ticket(101));
        Assert.Contains("t.subsidiary = 7", client.LastQuery);
        Assert.DoesNotContain("tosubsidiary", client.LastQuery);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Guard_rejects_missing_and_cross_subsidiary_ifs(bool returnOtherSubsidiary)
    {
        var client = new CapturingNetSuiteClient
        {
            NextItems = returnOtherSubsidiary
                ? [new OrdersDTO { NetsuiteOrderInternalId = 101, NetsuiteFromSubsidiaryInternalId = 8 }]
                : []
        };
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            TripTicketSubsidiaryGuard.ValidateAsync(client, Context(), Ticket(101)));
    }

    [Fact]
    public async Task Guard_rejects_forged_header_before_reading_or_writing()
    {
        var client = new CapturingNetSuiteClient();
        var data = Ticket(101);
        data.FromSubsidiary!.NetsuiteSubsidiaryInternalId = 8;
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => TripTicketSubsidiaryGuard.ValidateAsync(client, Context(), data));
        Assert.Empty(client.Calls);
    }

    [Fact]
    public async Task Guard_checks_every_batch_before_allowing_a_mixed_subsidiary_ticket()
    {
        var client = new CapturingNetSuiteClient
        {
            NextItems = Enumerable.Range(1, 500).Select(id => (object)new OrdersDTO
                { NetsuiteOrderInternalId = id, NetsuiteFromSubsidiaryInternalId = 7 }).ToList()
        };
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            TripTicketSubsidiaryGuard.ValidateAsync(client, Context(), Ticket(Enumerable.Range(1, 501).ToArray())));
        Assert.Equal(2, client.Calls.Count);
        Assert.Contains("t.id IN (501)", client.LastQuery);
    }

    [Fact]
    public async Task Guard_checks_removed_and_added_ifs_on_update()
    {
        var client = new CapturingNetSuiteClient
        {
            QueryItems = (query, _) => query.Contains("FROM customrecord_dbti_trip_ticket")
                ? [new OrdersDTO { NetsuiteOrderInternalId = 50 }]
                : [new OrdersDTO { NetsuiteOrderInternalId = 101, NetsuiteFromSubsidiaryInternalId = 7 }]
        };
        var data = Ticket(101);
        data.Id = 50;
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => TripTicketSubsidiaryGuard.ValidateAsync(
            client, Context(), data, [new ItemFulfillmentDTO { NetsuiteOrderInternalId = 102 }], existingTicket: true));
        Assert.Contains("t.id IN (101,102)", client.LastQuery);
    }

    [Fact]
    public async Task Service_rejects_missing_identity_before_any_external_call()
    {
        var service = new NetSuiteApiClientService(Context(null), null!, NullLogger<NetSuiteApiClientService>.Instance);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SaveTripTicket(Ticket(101)));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UpdateTripTicket(Ticket(101), [], []));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CancelTripTicket(Ticket(101)));
    }
}
