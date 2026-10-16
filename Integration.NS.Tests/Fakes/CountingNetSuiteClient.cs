using System.ComponentModel;
using Application.DataTransferObjects.Others.NS;
using Application.DataTransferObjects.Transactions.Commons.NS;
using Application.DataTransferObjects.Transactions.InventoryCounting.NS;
using Application.DataTransferObjects.Transactions.Packing.NS;
using Application.DataTransferObjects.Transactions.Receiving.NS;
using Application.DataTransferObjects.Transactions.TripTicket.NS;
using Application.UseCases.Repositories.Integration.Others;

namespace Integration.NS.Tests.Fakes;

/// <summary>
/// Minimal <see cref="INetSuiteApiClientService"/> stub that counts SuiteQL
/// executions. Transactional members are unsupported — the cache tests only
/// exercise reference reads.
/// </summary>
public sealed class CountingNetSuiteClient : INetSuiteApiClientService
{
    private int _executeSuiteQlCalls;

    public int ExecuteSuiteQlCalls => Volatile.Read(ref _executeSuiteQlCalls);

    public event PropertyChangedEventHandler? PropertyChanged;

    public Task<NetSuiteResponse<T>> ExecuteSuiteQLQuery<T>(string query, int? limit = null, int? offset = null, CancellationToken ct = default)
    {
        Interlocked.Increment(ref _executeSuiteQlCalls);
        return Task.FromResult(new NetSuiteResponse<T>
        {
            items = new List<T>(),
            totalResults = 0
        });
    }

    public Task<IEnumerable<T>?> NetsuiteQuery<T>(
        string queryName,
        Dictionary<string, string>? parameters = null,
        int limit = 0,
        int offset = 0,
        CancellationToken ct = default)
        => throw new NotSupportedException();

    public Task<T> MakeRequest<T>(
        string url,
        string? reqBody,
        HttpMethod method,
        NetSuiteRequestKind? kind = null,
        NetSuiteRetryProfile profile = NetSuiteRetryProfile.Interactive,
        CancellationToken ct = default)
        => throw new NotSupportedException();

    public Task<T> MakeRequestOAuth1<T>(
        string url,
        string? reqBody,
        HttpMethod method,
        NetSuiteRequestKind? kind = null,
        NetSuiteRetryProfile profile = NetSuiteRetryProfile.Interactive,
        CancellationToken ct = default)
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
