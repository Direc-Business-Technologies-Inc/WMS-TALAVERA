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
/// <see cref="INetSuiteApiClientService"/> stub that captures the REST write
/// bodies issued through <see cref="MakeRequest{T}"/> so payload shape can be
/// asserted without a live NetSuite connection.
/// </summary>
public sealed class RestCapturingNetSuiteClient : INetSuiteApiClientService
{
    public List<(string Url, string? Body, HttpMethod Method)> Writes { get; } = new();

    public string? LastBody => Writes.Count == 0 ? null : Writes[^1].Body;

    public string? LastUrl => Writes.Count == 0 ? null : Writes[^1].Url;

    public HttpMethod? LastMethod => Writes.Count == 0 ? null : Writes[^1].Method;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Task<NetSuiteResponse<T>> ExecuteSuiteQLQuery<T>(string query, int? limit = null, int? offset = null, CancellationToken ct = default)
        => Task.FromResult(new NetSuiteResponse<T>
        {
            items = new List<T>(),
            totalResults = 0
        });

    public Task<T> MakeRequest<T>(
        string url,
        string? reqBody,
        HttpMethod method,
        NetSuiteRequestKind? kind = null,
        NetSuiteRetryProfile profile = NetSuiteRetryProfile.Interactive,
        CancellationToken ct = default)
    {
        Writes.Add((url, reqBody, method));
        return Task.FromResult(default(T)!);
    }

    public Task<IEnumerable<T>?> NetsuiteQuery<T>(
        string queryName,
        Dictionary<string, string>? parameters = null,
        int limit = 0,
        int offset = 0,
        CancellationToken ct = default)
        => throw new NotSupportedException();

    public Task<NetSuiteResponse<T>?> NetsuiteQueryWithMetadata<T>(
        string queryName,
        Dictionary<string, string>? parameters = null,
        int limit = 0,
        int offset = 0,
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