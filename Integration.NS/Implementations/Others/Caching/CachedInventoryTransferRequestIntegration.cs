using Application.DataTransferObjects.Transactions.InventoryTransferRequest;
using Application.UseCases.Repositories.Integration.Transaction.InventoryTransferRequest;
using Shared.Entities;

namespace Integration.NS.Implementations.Others.Caching;

/// <summary>
/// 60-second grid cache for inventory transfer requests (Req 2.1). Only the
/// paginated grid reads are cached; document reads and mutations pass straight
/// through, and a successful mutation evicts the entity's cached grid pages
/// (Req 2.2).
/// </summary>
public class CachedInventoryTransferRequestIntegration(
    Transactions.InventoryTransferRequestIntegration inner,
    GridResultCache cache) : IInventoryTransferRequestIntegration
{
    public const string EntityName = "InventoryTransferRequest";

    public Task<(IEnumerable<InventoryTransferRequestDataGridDTO> Data, int Count)> GetInventoryTransferRequestsDataGridAsync(DataGridIntent intent)
        => cache.GetOrAddAsync(EntityName, nameof(GetInventoryTransferRequestsDataGridAsync), intent,
            () => inner.GetInventoryTransferRequestsDataGridAsync(intent));

    public Task<IEnumerable<InventoryTransferRequestLineDTO>> GetInventoryTransferRequestLinesAsync(string @ref)
        => inner.GetInventoryTransferRequestLinesAsync(@ref);

    public Task<InventoryTransferRequestDTO?> GetInventoryTransferRequestAsync(string @ref)
        => inner.GetInventoryTransferRequestAsync(@ref);

    public Task<(IEnumerable<InventoryTransferRequestStatusDTO>, int)> GetStatusTypesAsync(DataGridIntent intent)
        => inner.GetStatusTypesAsync(intent);

    public async Task<bool> CreateInventoryTransferRequest(InventoryTransferRequestDTO data)
        => await Mutate(() => inner.CreateInventoryTransferRequest(data));

    public async Task<bool> UpdateInventoryTransferRequest(InventoryTransferRequestDTO data)
        => await Mutate(() => inner.UpdateInventoryTransferRequest(data));

    public async Task<bool> SubmitInventoryTransferRequestForApproval(InventoryTransferRequestDTO data)
        => await Mutate(() => inner.SubmitInventoryTransferRequestForApproval(data));

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