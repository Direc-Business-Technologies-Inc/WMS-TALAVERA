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
/// the NetSuite vendor internal id, which is exactly what the line stores in
/// <c>transactionline.custcol_dbti_vendor</c> — and only that vendor's lines are
/// eligible for fulfillment.
/// <para>
/// The filter is applied in <see cref="MGetTransferOrderLineQryHandler"/> rather
/// than in <c>NS_TO_x_Packing_Get_Items.sql</c>, because that script is also
/// called by the fulfillment save path, which must still see every line (it posts
/// zero-quantity entries for the lines it was not asked to fulfill). Adding an
/// optional <c>@vendorId</c> token would also break callers that do not supply it:
/// unsubstituted tokens reach NetSuite as invalid SQL.
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

    private static async Task<List<TransferOrderLineDTO>> QueryAsync(int? vendorId, FakeNetSuiteClient client)
    {
        var handler = new MGetTransferOrderLineQryHandler(client);

        var result = await handler.Handle(
            new GetTransferOrderLineQry(new TransferOrderLineRequestDTO
            {
                OrderNumber = "ISTR-THM00100000776-2",
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
    public async Task Vendor_never_travels_into_the_script_parameters()
    {
        var client = new FakeNetSuiteClient();
        client.Lines.Add(Line(1, 111));

        await QueryAsync(vendorId: 111, client);

        Assert.NotNull(client.LastParameters);
        Assert.Equal("ISTR-THM00100000776-2", client.LastParameters!["tranid"]);
        Assert.False(client.LastParameters.ContainsKey("vendorId"));
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
            return Task.FromResult<IEnumerable<T>?>(Lines.OfType<T>().ToList());
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
