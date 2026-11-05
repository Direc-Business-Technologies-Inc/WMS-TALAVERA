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
/// Vendor gate on the packing line list used by the mobile app. The operator scans
/// the vendor barcode printed under the STR's Vendor field — the barcode carries
/// the vendor <em>name</em>, which is resolved to its NetSuite id before this
/// query runs.
/// <para>
/// The gate's scope is applied by <c>NS_TO_x_Packing_Get_Items.sql</c>, which
/// takes an optional <c>{vendorId}</c> integer splice. A scoped call sends the
/// vendor id; a call with no scope sends the <c>"0"</c> sentinel and receives
/// every line. The handler performs no in-memory filter, so a broken predicate
/// cannot be masked: these tests assert the parameter shape that reaches the
/// script, and the fake applies the script's scope the way SuiteQL does.
/// </para>
/// </summary>
public class StrPackingVendorFilterShapeTests
{
    private static TransferOrderLineDTO Line(int lineNumber, int? vendorId, string? vendorName = null) => new()
    {
        LineSequenceNumber = lineNumber,
        VendorId = vendorId,
        VendorName = vendorName,
        MaterialCode = $"ITEM-{lineNumber}"
    };

    private const string Tranid = "ISTR-THM00100000776-2";

    private static async Task<List<TransferOrderLineDTO>> QueryAsync(int? vendorId, FakeNetSuiteClient client)
    {
        var handler = new MGetTransferOrderLineQryHandler(client);

        var result = await handler.Handle(
            new GetTransferOrderLineQry(new TransferOrderLineRequestDTO
            {
                OrderNumber = Tranid,
                VendorId = vendorId
            }),
            CancellationToken.None);

        return result.ToList();
    }

    [Fact]
    public async Task Scanned_vendor_returns_only_that_vendors_lines()
    {
        var client = new FakeNetSuiteClient();
        client.Lines.AddRange([Line(1, 111, "VENDOR A"), Line(2, 222, "VENDOR B"), Line(3, 111, "VENDOR A")]);

        var lines = await QueryAsync(vendorId: 111, client);

        Assert.Equal(2, lines.Count);
        Assert.All(lines, line => Assert.Equal(111, line.VendorId));
    }

    [Fact]
    public async Task No_vendor_scanned_returns_every_line()
    {
        var client = new FakeNetSuiteClient();
        client.Lines.AddRange([Line(1, 111), Line(2, 222), Line(3, null)]);

        var lines = await QueryAsync(vendorId: null, client);

        Assert.Equal(3, lines.Count);
    }

    [Fact]
    public async Task Lines_without_a_vendor_are_excluded_when_a_vendor_is_scanned()
    {
        var client = new FakeNetSuiteClient();
        client.Lines.AddRange([Line(1, 111), Line(2, null)]);

        var lines = await QueryAsync(vendorId: 111, client);

        var line = Assert.Single(lines);
        Assert.Equal(1, line.LineSequenceNumber);
    }

    [Fact]
    public async Task Tranid_travels_as_a_text_parameter()
    {
        var client = new FakeNetSuiteClient();
        client.Lines.Add(Line(1, 111));

        await QueryAsync(vendorId: 111, client);

        Assert.NotNull(client.LastParameters);
        Assert.Equal(Tranid, client.LastParameters!["tranid"]);
    }

    [Fact]
    public async Task Scanned_vendor_travels_as_the_vendor_scope()
    {
        var client = new FakeNetSuiteClient();
        client.Lines.Add(Line(1, 111));

        await QueryAsync(vendorId: 111, client);

        Assert.NotNull(client.LastParameters);
        Assert.Equal("111", client.LastParameters!["vendorId"]);
    }

    [Fact]
    public async Task No_vendor_scanned_sends_the_zero_sentinel()
    {
        var client = new FakeNetSuiteClient();
        client.Lines.Add(Line(1, 111));

        await QueryAsync(vendorId: null, client);

        Assert.NotNull(client.LastParameters);
        Assert.Equal("0", client.LastParameters!["vendorId"]);
    }

    private sealed class FakeNetSuiteClient : INetSuiteApiClientService
    {
        public List<TransferOrderLineDTO> Lines { get; } = [];
        public Dictionary<string, string>? LastParameters { get; private set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public Task<IEnumerable<T>?> NetsuiteQuery<T>(
            string queryName,
            Dictionary<string, string>? parameters = null,
            int limit = 0,
            int offset = 0,
            CancellationToken ct = default)
        {
            LastParameters = parameters;
            return Task.FromResult<IEnumerable<T>?>(Scoped<T>(parameters));
        }

        public Task<NetSuiteResponse<T>?> NetsuiteQueryWithMetadata<T>(
            string queryName,
            Dictionary<string, string>? parameters = null,
            int limit = 0,
            int offset = 0,
            CancellationToken ct = default)
        {
            LastParameters = parameters;
            var items = Scoped<T>(parameters);
            return Task.FromResult<NetSuiteResponse<T>?>(new NetSuiteResponse<T>
            {
                items = items,
                count = items.Count,
                totalResults = items.Count,
                hasMore = false
            });
        }

        // The script's predicate is {vendorId} = 0 OR custcol_dbti_vendor = {vendorId}.
        private List<T> Scoped<T>(Dictionary<string, string>? parameters)
        {
            var items = Lines.OfType<T>().ToList();

            if (typeof(T) == typeof(TransferOrderLineDTO) &&
                parameters?.TryGetValue("vendorId", out var raw) == true &&
                int.TryParse(raw, out var vendorId) && vendorId > 0)
            {
                items = ((IEnumerable<TransferOrderLineDTO>)items)
                    .Where(line => line.VendorId == vendorId)
                    .Cast<T>()
                    .ToList();
            }

            return items;
        }

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
