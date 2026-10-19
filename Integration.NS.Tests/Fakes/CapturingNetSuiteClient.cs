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
/// <see cref="INetSuiteApiClientService"/> stub that captures the last SuiteQL
/// text and the limit/offset of every call, and returns a caller-supplied result.
/// Used to assert query shape, limit/offset bounds, and DTO mapping without a
/// live NetSuite connection.
/// </summary>
public sealed class CapturingNetSuiteClient : INetSuiteApiClientService
{
    public string? LastQuery { get; private set; }
    public int? LastLimit { get; private set; }
    public int? LastOffset { get; private set; }
    /// <summary>(limit, offset) of every <see cref="ExecuteSuiteQLQuery{T}"/> call, in order.</summary>
    public List<(int? Limit, int? Offset)> Calls { get; } = [];
    public object? NextResult { get; set; }
    public List<object> NextItems { get; set; } = [];
    public int CallCount { get; private set; }
    /// <summary>Thrown once by the next ExecuteSuiteQLQuery call (after the call is recorded), then cleared.</summary>
    public Exception? NextException { get; set; }
    /// <summary>
    /// When set, the next <see cref="ExecuteSuiteQLQuery{T}"/> returns a response whose
    /// <c>items</c> is null instead of an array, reproducing a 2xx SuiteQL body that omits
    /// the array entirely (error envelope, partial response). <see cref="MakeRequest{T}"/>
    /// deserializes such a body into a non-null <see cref="NetSuiteResponse{T}"/> with a null
    /// <c>items</c>, which is what consumers must defend against.
    /// </summary>
    public bool NextNullItems { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Task<NetSuiteResponse<T>> ExecuteSuiteQLQuery<T>(string query, int? limit = null, int? offset = null, CancellationToken ct = default)
    {
        CallCount++;
        LastQuery = query;
        LastLimit = limit;
        LastOffset = offset;
        Calls.Add((limit, offset));

        if (NextException is not null)
        {
            var exception = NextException;
            NextException = null;
            throw exception;
        }

        if (NextNullItems)
        {
            NextNullItems = false;
            return Task.FromResult(new NetSuiteResponse<T>
            {
                items = null!,
                totalResults = 0
            });
        }

        var items = new List<T>();
        foreach (var next in NextItems.OfType<T>()) items.Add(next);
        if (NextResult is T typed) items.Add(typed);
        return Task.FromResult(new NetSuiteResponse<T>
        {
            items = items,
            totalResults = items.Count
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
