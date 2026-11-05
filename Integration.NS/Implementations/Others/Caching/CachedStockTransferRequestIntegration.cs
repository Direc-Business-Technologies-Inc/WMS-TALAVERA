using Application.DataTransferObjects.Transactions.StockTransferRequest;
using Application.UseCases.Repositories.Integration.Transaction.StockTransferRequest;
using Shared.Entities;

namespace Integration.NS.Implementations.Others.Caching;

/// <summary>
/// 60-second grid cache for stock transfer requests (Req 2.1). Only the
/// paginated grid reads are cached; document reads and mutations pass straight
/// through, and a successful mutation evicts the entity's cached grid pages
/// (Req 2.2).
/// </summary>
internal class CachedStockTransferRequestIntegration(
    Transactions.StockTransferRequestIntegration inner,
    GridResultCache cache) : IStockTransferRequestIntegration
{
    public const string EntityName = "StockTransferRequest";

    public Task<(IEnumerable<StockTransferRequestDataGridDTO> Data, int Count)> GetTransferOrderList(DataGridIntent intent)
        => cache.GetOrAddAsync(EntityName, nameof(GetTransferOrderList), intent,
            () => inner.GetTransferOrderList(intent));

    public Task<(IEnumerable<StockTransferRequestDataGridDTO> Data, int Count)> GetIntercompanyTransferOrderList(DataGridIntent intent)
        => cache.GetOrAddAsync(EntityName, nameof(GetIntercompanyTransferOrderList), intent,
            () => inner.GetIntercompanyTransferOrderList(intent));

    public Task<(IEnumerable<StockTransferRequestDataGridDTO> Data, int Count)> GetReturnsList(DataGridIntent intent)
        => cache.GetOrAddAsync(EntityName, nameof(GetReturnsList), intent,
            () => inner.GetReturnsList(intent));

    public Task<(IEnumerable<TransferOrderStatus> data, int count)> GetTransferOrderStatuses(DataGridIntent intent)
        => inner.GetTransferOrderStatuses(intent);

    public Task<StockTransferRequestInfoDTO?> GetStockTransferRequest(string id)
        => inner.GetStockTransferRequest(id);

    public Task<IEnumerable<StockTransferRequestLineDTO>?> GetStockTransferRequestLines(string id)
        => inner.GetStockTransferRequestLines(id);

    public async Task<bool> CreateStockTransferRequest(StockTransferRequestInfoDTO dto)
        => await Mutate(() => inner.CreateStockTransferRequest(dto));

    public async Task<bool> UpdateStockTransferRequest(StockTransferRequestInfoDTO dto)
        => await Mutate(() => inner.UpdateStockTransferRequest(dto));

    public async Task<bool> SubmitStockTransferRequestForApproval(StockTransferRequestInfoDTO dto)
        => await Mutate(() => inner.SubmitStockTransferRequestForApproval(dto));

    private async Task<bool> Mutate(Func<Task<bool>> mutation)
    {
        var result = await mutation();
        if (result)
        {
            cache.Invalidate(EntityName);
        }

        return result;
    }
}