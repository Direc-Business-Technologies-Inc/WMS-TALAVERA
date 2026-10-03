using System.ComponentModel;
using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.DataTransferObjects.Transactions.InventoryCounting.NS;
using Application.DataTransferObjects.Transactions.Packing;
using Application.DataTransferObjects.Transactions.Packing.NS;
using Application.DataTransferObjects.Transactions.Receiving.NS;
using Application.DataTransferObjects.Transactions.TripTicket.NS;
using Application.UseCases.Commands.Transaction.Packing.NS.Returns;
using Application.UseCases.Commands.Transaction.Packing.NS.TransferOrder;
using Application.UseCases.Commands.Transaction.Packing.NS.VendorReturnAuthorization;
using Application.UseCases.Repositories.Integration.Others;
using Application.UseCases.Repositories.Integration.Transaction.Packing;
using Xunit;

namespace Application.UseCases.Tests;

public sealed class ItemFulfillmentCommandHardeningTests
{
    [Fact]
    public async Task Transfer_order_uses_server_resolved_shipped_status()
    {
        var netSuite = new RecordingNetSuiteApiClient();
        var resolver = new StubItemFulfillmentStatusResolver(ItemFulfillmentShipStatus.Shipped);
        var handler = new PostTransferOrderIFCmdHandler(netSuite, resolver);
        var lines = new List<PostTransferOrderDTO>
        {
            new() { NetsuiteOrderInternalId = 101 },
            new() { NetsuiteOrderInternalId = 101 }
        };

        var result = await handler.Handle(new PostTransferOrderIFCmd(lines), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(101, resolver.SourceTransactionId);
        Assert.Equal(ItemFulfillmentShipStatus.Shipped, netSuite.TransferOrderStatus);
        Assert.Equal(1, netSuite.TransferOrderCalls);
    }

    [Fact]
    public async Task Returns_uses_server_resolved_packed_status()
    {
        var netSuite = new RecordingNetSuiteApiClient();
        var resolver = new StubItemFulfillmentStatusResolver(ItemFulfillmentShipStatus.Packed);
        var handler = new PostReturnsIFCmdHandler(netSuite, resolver);
        var lines = new List<PostReturnsDTO>
        {
            new() { NetsuiteOrderInternalId = 202 }
        };

        var result = await handler.Handle(new PostReturnsIFCmd(lines), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(202, resolver.SourceTransactionId);
        Assert.Equal(ItemFulfillmentShipStatus.Packed, netSuite.ReturnsStatus);
        Assert.Equal(1, netSuite.ReturnsCalls);
    }

    [Fact]
    public async Task Vendor_return_uses_server_resolved_shipped_status()
    {
        var netSuite = new RecordingNetSuiteApiClient();
        var resolver = new StubItemFulfillmentStatusResolver(ItemFulfillmentShipStatus.Shipped);
        var handler = new PostVendorReturnAuthorizationIFCmdHandler(netSuite, resolver);
        var lines = new List<PostVendorReturnAuthorizationDTO>
        {
            new() { NetsuiteOrderInternalId = 303 }
        };

        var result = await handler.Handle(
            new PostVendorReturnAuthorizationIFCmd(lines),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(303, resolver.SourceTransactionId);
        Assert.Equal(ItemFulfillmentShipStatus.Shipped, netSuite.VendorReturnStatus);
        Assert.Equal(1, netSuite.VendorReturnCalls);
    }

    [Fact]
    public async Task Mixed_source_transactions_are_rejected_before_lookup_or_posting()
    {
        var netSuite = new RecordingNetSuiteApiClient();
        var resolver = new StubItemFulfillmentStatusResolver(ItemFulfillmentShipStatus.Shipped);
        var handler = new PostReturnsIFCmdHandler(netSuite, resolver);
        var lines = new List<PostReturnsDTO>
        {
            new() { NetsuiteOrderInternalId = 1 },
            new() { NetsuiteOrderInternalId = 2 }
        };

        var result = await handler.Handle(new PostReturnsIFCmd(lines), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(0, resolver.Calls);
        Assert.Equal(0, netSuite.ReturnsCalls);
    }

    [Fact]
    public async Task Empty_fulfillment_is_rejected_before_lookup_or_posting()
    {
        var netSuite = new RecordingNetSuiteApiClient();
        var resolver = new StubItemFulfillmentStatusResolver(ItemFulfillmentShipStatus.Packed);
        var handler = new PostTransferOrderIFCmdHandler(netSuite, resolver);

        var result = await handler.Handle(
            new PostTransferOrderIFCmd([]),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(0, resolver.Calls);
        Assert.Equal(0, netSuite.TransferOrderCalls);
    }

    [Fact]
    public async Task Resolver_failure_stops_posting_and_returns_server_error()
    {
        var netSuite = new RecordingNetSuiteApiClient();
        var resolver = new StubItemFulfillmentStatusResolver(
            new InvalidOperationException("Exemption lookup failed."));
        var handler = new PostVendorReturnAuthorizationIFCmdHandler(netSuite, resolver);

        var result = await handler.Handle(
            new PostVendorReturnAuthorizationIFCmd(
                [new PostVendorReturnAuthorizationDTO { NetsuiteOrderInternalId = 404 }]),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(500, result.StatusCode);
        Assert.Contains("Exemption lookup failed", result.ErrorMessage);
        Assert.Equal(0, netSuite.VendorReturnCalls);
    }

    private sealed class StubItemFulfillmentStatusResolver : IItemFulfillmentStatusResolver
    {
        private readonly ItemFulfillmentShipStatus _status;
        private readonly Exception? _exception;

        public StubItemFulfillmentStatusResolver(ItemFulfillmentShipStatus status) => _status = status;

        public StubItemFulfillmentStatusResolver(Exception exception) => _exception = exception;

        public int Calls { get; private set; }
        public int? SourceTransactionId { get; private set; }

        public Task<ItemFulfillmentShipStatus> ResolveAsync(
            int sourceTransactionId,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            SourceTransactionId = sourceTransactionId;

            if (_exception is not null)
                throw _exception;

            return Task.FromResult(_status);
        }
    }

    private sealed class RecordingNetSuiteApiClient : INetSuiteApiClientService
    {
        public event PropertyChangedEventHandler? PropertyChanged
        {
            add { }
            remove { }
        }

        public string GetRestAPIURI => string.Empty;
        public string GetRestletURI => string.Empty;

        public int TransferOrderCalls { get; private set; }
        public int ReturnsCalls { get; private set; }
        public int VendorReturnCalls { get; private set; }
        public ItemFulfillmentShipStatus? TransferOrderStatus { get; private set; }
        public ItemFulfillmentShipStatus? ReturnsStatus { get; private set; }
        public ItemFulfillmentShipStatus? VendorReturnStatus { get; private set; }

        public Task<bool> SaveTOItemFulfillment(
            List<PostTransferOrderDTO> data,
            ItemFulfillmentShipStatus status)
        {
            TransferOrderCalls++;
            TransferOrderStatus = status;
            return Task.FromResult(true);
        }

        public Task<bool> SaveReturnsItemFulfillment(
            List<PostReturnsDTO> data,
            ItemFulfillmentShipStatus status)
        {
            ReturnsCalls++;
            ReturnsStatus = status;
            return Task.FromResult(true);
        }

        public Task<bool> SaveVRAItemFulfillment(
            List<PostVendorReturnAuthorizationDTO> data,
            ItemFulfillmentShipStatus status)
        {
            VendorReturnCalls++;
            VendorReturnStatus = status;
            return Task.FromResult(true);
        }

        public Task<NetSuiteResponse<T>> ExecuteSuiteQLQuery<T>(string query, int? limit = null, int? offset = null) =>
            throw new NotSupportedException();

        public Task<IEnumerable<T>?> NetsuiteQuery<T>(
            string queryName,
            Dictionary<string, string>? parameters = null,
            int limit = 0,
            int offset = 0) => throw new NotSupportedException();

        public Task<T> MakeRequest<T>(string url, string? reqBody, HttpMethod method) =>
            throw new NotSupportedException();

        public Task<T> MakeRequestOAuth1<T>(string url, string? reqBody, HttpMethod method) =>
            throw new NotSupportedException();

        public Task<bool> UpdateTripTicket(
            PostTripTicketDTO data,
            List<ItemFulfillmentDTO> removedIF,
            List<ItemFulfillmentDTO> addedIF) => throw new NotSupportedException();

        public Task<bool> CancelTripTicket(PostTripTicketDTO data) => throw new NotSupportedException();
        public Task<bool> SaveTripTicket(PostTripTicketDTO data) => throw new NotSupportedException();
        public Task<bool> SavePOItemReceipt(List<PostPurchaseOrderDTO> data, int userId, string remarks) => throw new NotSupportedException();
        public Task<bool> SaveTOItemReceipt(List<PostTransferOrderDTO> data, int orderId, int userId, string remarks) => throw new NotSupportedException();
        public Task<bool> SaveReturnsItemReceipt(List<PostReturnsDTO> data, int orderId, int userId, string remarks) => throw new NotSupportedException();
        public Task<bool> PatchInventoryCounting(List<PatchInventoryCountingDTO> data) => throw new NotSupportedException();
        public Task<bool> PostInventoryWorksheet(List<InventoryWorksheetLineDTO> data, int location, int subsidiary) => throw new NotSupportedException();
    }
}
