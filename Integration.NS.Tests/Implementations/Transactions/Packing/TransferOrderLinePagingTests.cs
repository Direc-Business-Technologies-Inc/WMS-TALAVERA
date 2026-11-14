using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.DataTransferObjects.Transactions.Commons.NS.Request;
using Application.DataTransferObjects.Transactions.InventoryCounting.NS;
using Application.DataTransferObjects.Transactions.Packing.NS;
using Application.DataTransferObjects.Transactions.Receiving.NS;
using Application.DataTransferObjects.Transactions.TripTicket.NS;
using Application.UseCases.Queries.Transaction.Packing.NS.TransferOrder;
using Application.UseCases.Repositories.Integration.Others;
using System.ComponentModel;
using Xunit;

namespace Integration.NS.Tests.Implementations.Transactions.Packing;

/// <summary>
/// The packing line read used to issue one unpaged SuiteQL call, so NetSuite's
/// 1,000-row page cap silently truncated every larger transfer order. The handler
/// now pages with a stable order and stops on the response's authoritative
/// <c>totalResults</c> instead of probing past the end: NetSuite answers an offset
/// equal to the result count with 404 INVALID_PARAMETER, not an empty page, so an
/// exact multiple of the page size must still return in one call.
/// </summary>
public class TransferOrderLinePagingTests
{
    private const string TranId = "ISTR-THM00100000776-2";

    private static TransferOrderLineDTO Line(int lineNumber) => new()
    {
        LineSequenceNumber = lineNumber,
        MaterialCode = $"ITEM-{lineNumber}"
    };

    private static async Task<List<TransferOrderLineDTO>> QueryAsync(PagingFakeClient client)
    {
        var handler = new MGetTransferOrderLineQryHandler(client);

        var result = await handler.Handle(
            new GetTransferOrderLineQry(new TransferOrderLineRequestDTO
            {
                OrderNumber = TranId
            }),
            CancellationToken.None);

        return result.ToList();
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(999, 1)]
    [InlineData(1000, 1)]
    [InlineData(1001, 2)]
    [InlineData(2000, 2)]
    public async Task Reads_every_line_with_the_expected_call_count(int total, int expectedCalls)
    {
        var client = new PagingFakeClient(total);

        var lines = await QueryAsync(client);

        Assert.Equal(total, lines.Count);
        Assert.Equal(expectedCalls, client.Calls.Count);
        Assert.Equal(Enumerable.Range(1, total), lines.Select(line => line.LineSequenceNumber));
        Assert.Equal(total, lines.Select(line => line.LineSequenceNumber).Distinct().Count());
    }

    [Fact]
    public async Task Pages_are_requested_with_a_fixed_size_and_increasing_offsets()
    {
        var client = new PagingFakeClient(2000);

        await QueryAsync(client);

        Assert.Equal([(1000, 0), (1000, 1000)], client.Calls);
    }

    [Fact]
    public async Task Tranid_is_passed_to_the_script()
    {
        var client = new PagingFakeClient(1);

        await QueryAsync(client);

        Assert.NotNull(client.LastParameters);
        Assert.Equal(TranId, client.LastParameters!["tranid"]);
    }

    [Fact]
    public async Task A_page_failure_fails_the_whole_read_instead_of_returning_a_partial_list()
    {
        var client = new PagingFakeClient(2000) { FailOnCall = 2 };

        await Assert.ThrowsAsync<InvalidOperationException>(() => QueryAsync(client));

        Assert.Equal(2, client.Calls.Count);
    }

    [Fact]
    public async Task A_short_page_that_contradicts_total_results_fails_instead_of_returning_a_partial_list()
    {
        var client = new PagingFakeClient(total: 1657) { ShortPageOnCall = 2 };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => QueryAsync(client));

        Assert.Contains(TranId, exception.Message);
        Assert.Contains("1,001 of 1,657", exception.Message);
        Assert.Equal([(1000, 0), (1000, 1000)], client.Calls);
    }

    [Fact]
    public async Task A_response_without_total_results_still_stops_on_a_short_page()
    {
        var client = new PagingFakeClient(1657) { OmitTotalResults = true };

        var lines = await QueryAsync(client);

        Assert.Equal(1657, lines.Count);
        Assert.Equal([(1000, 0), (1000, 1000)], client.Calls);
    }

    [Fact]
    public async Task A_runaway_full_page_loop_fails_loudly()
    {
        var client = new PagingFakeClient(total: 0)
        {
            OmitTotalResults = true,
            AlwaysReturnFullPage = true
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => QueryAsync(client));

        Assert.Contains(TranId, exception.Message);
        Assert.Equal(100, client.Calls.Count);
    }

    [Fact]
    public async Task Cancellation_is_forwarded_to_the_transport()
    {
        var client = new PagingFakeClient(1);
        using var cancellation = new CancellationTokenSource();

        var handler = new MGetTransferOrderLineQryHandler(client);
        await handler.Handle(
            new GetTransferOrderLineQry(new TransferOrderLineRequestDTO { OrderNumber = TranId }),
            cancellation.Token);

        Assert.Equal(cancellation.Token, client.LastCancellationToken);
    }

    private sealed class PagingFakeClient(int total) : INetSuiteApiClientService
    {
        public List<(int Limit, int Offset)> Calls { get; } = [];
        public Dictionary<string, string>? LastParameters { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }
        public int? FailOnCall { get; init; }
        /// <summary>
        /// Return fewer rows than the offset/limit window allows on this call while
        /// still reporting the full total: the inconsistency that must fail loudly
        /// rather than be accepted as the end of the read.
        /// </summary>
        public int? ShortPageOnCall { get; init; }
        public bool OmitTotalResults { get; init; }
        public bool AlwaysReturnFullPage { get; init; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public Task<NetSuiteResponse<T>?> NetsuiteQueryWithMetadata<T>(
            string queryName,
            Dictionary<string, string>? parameters = null,
            int limit = 0,
            int offset = 0,
            CancellationToken ct = default)
        {
            Calls.Add((limit, offset));
            LastParameters = parameters;
            LastCancellationToken = ct;

            if (FailOnCall == Calls.Count)
            {
                throw new InvalidOperationException("Synthetic page failure.");
            }

            var pageSize = AlwaysReturnFullPage ? limit : Math.Max(0, Math.Min(limit, total - offset));
            if (ShortPageOnCall == Calls.Count)
            {
                pageSize = Math.Min(pageSize, 1);
            }

            var items = Enumerable.Range(offset + 1, pageSize)
                .Select(Line)
                .OfType<T>()
                .ToList();

            return Task.FromResult<NetSuiteResponse<T>?>(new NetSuiteResponse<T>
            {
                items = items,
                count = items.Count,
                totalResults = OmitTotalResults ? 0 : total,
                hasMore = AlwaysReturnFullPage || offset + limit < total
            });
        }

        public Task<IEnumerable<T>?> NetsuiteQuery<T>(
            string queryName,
            Dictionary<string, string>? parameters = null,
            int limit = 0,
            int offset = 0,
            CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<NetSuiteResponse<T>> ExecuteSuiteQLQuery<T>(string query, int? limit = null, int? offset = null, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<T> MakeRequest<T>(string url, string? reqBody, HttpMethod method, NetSuiteRequestKind? kind = null, NetSuiteRetryProfile profile = NetSuiteRetryProfile.Interactive, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<T> MakeRequestOAuth1<T>(string url, string? reqBody, HttpMethod method, NetSuiteRequestKind? kind = null, NetSuiteRetryProfile profile = NetSuiteRetryProfile.Interactive, CancellationToken ct = default)
            => throw new NotSupportedException();

        public string GetRestAPIURI => string.Empty;
        public string GetRestletURI => string.Empty;

        public Task<bool> SaveTOItemFulfillment(List<PostTransferOrderDTO> Data) => throw new NotSupportedException();
        public Task<bool> SaveReturnsItemFulfillment(List<PostReturnsDTO> Data) => throw new NotSupportedException();
        public Task<bool> SaveVRAItemFulfillment(List<PostVendorReturnAuthorizationDTO> Data) => throw new NotSupportedException();
        public Task<bool> UpdateTripTicket(PostTripTicketDTO Data, List<ItemFulfillmentDTO> RemovedIF, List<ItemFulfillmentDTO> AddedIF) => throw new NotSupportedException();
        public Task<bool> CancelTripTicket(PostTripTicketDTO Data) => throw new NotSupportedException();
        public Task<bool> SaveTripTicket(PostTripTicketDTO Data) => throw new NotSupportedException();
        public Task<bool> SavePOItemReceipt(List<PostPurchaseOrderDTO> Data, int userId, string remarks) => throw new NotSupportedException();
        public Task<bool> SaveTOItemReceipt(List<PostTransferOrderDTO> Data, int TONetsuiteOrderInternalId, int userId, string remarks) => throw new NotSupportedException();
        public Task<bool> SaveReturnsItemReceipt(List<PostReturnsDTO> Data, int TONetsuiteOrderInternalId, int userId, string remarks) => throw new NotSupportedException();
        public Task<bool> PatchInventoryCounting(List<PatchInventoryCountingDTO> Data) => throw new NotSupportedException();
        public Task<bool> PostInventoryWorksheet(List<InventoryWorksheetLineDTO> Data, int Location, int Subsidiary) => throw new NotSupportedException();
    }
}
