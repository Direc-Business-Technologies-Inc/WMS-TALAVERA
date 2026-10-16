using Application.DataTransferObjects.Transactions.Receiving;
using Application.DataTransferObjects.Transactions.SupplierReturn;
using Application.UseCases.Repositories.Integration.Transaction.SupplierReturn;
using Shared.Entities;

namespace Integration.NS.Implementations.Others.Caching;

/// <summary>
/// 60-second grid cache for supplier returns (Req 2.1). Only the paginated grid
/// reads are cached; document reads and mutations pass straight through, and a
/// successful mutation evicts the entity's cached grid pages (Req 2.2).
/// </summary>
public class CachedSupplierReturnIntegration(
    Transactions.SupplierReturnIntegration inner,
    GridResultCache cache) : ISupplierReturnIntegration
{
    public const string EntityName = "SupplierReturn";

    public Task<(IEnumerable<SupplierReturnDataGridDTO> Data, int Count)> GetReturnsDataGridAsync(DataGridIntent intent)
        => cache.GetOrAddAsync(EntityName, nameof(GetReturnsDataGridAsync), intent,
            () => inner.GetReturnsDataGridAsync(intent));

    public Task<(IEnumerable<PurchaseOrderDataGridDTO>, int)> GetPurchaseOrdersListAsync(DataGridIntent intent)
        => cache.GetOrAddAsync(EntityName, nameof(GetPurchaseOrdersListAsync), intent,
            () => inner.GetPurchaseOrdersListAsync(intent));

    public Task<(IEnumerable<ReturnCategoryDTO> Data, int Count)> GetReturnCategories(DataGridIntent intent)
        => inner.GetReturnCategories(intent);

    public Task<(IEnumerable<ReturnStatusDTO> Data, int Count)> GetReturnStatuses(DataGridIntent intent)
        => inner.GetReturnStatuses(intent);

    public Task<(IEnumerable<PurchaseSubCategoryDTO>, int count)> GetPurchaseSubcategoriesAsync(DataGridIntent intent)
        => inner.GetPurchaseSubcategoriesAsync(intent);

    public Task<(IEnumerable<PurchaseCategoryDTO>, int count)> GetPurchaseCategoriesAsync(DataGridIntent intent)
        => inner.GetPurchaseCategoriesAsync(intent);

    public Task<SupplierReturnDTO?> GetReturnAsync(string referenceNumber)
        => inner.GetReturnAsync(referenceNumber);

    public Task<IEnumerable<SupplierReturnLineDTO>> GetReturnLinesAsync(string referenceNumber)
        => inner.GetReturnLinesAsync(referenceNumber);

    public Task<SupplierReturnDTO?> GetReturnFromPurchaseOrderAsync(string purchaseOrderId)
        => inner.GetReturnFromPurchaseOrderAsync(purchaseOrderId);

    public Task<IEnumerable<SupplierReturnLineDTO>> GetReturnFromPurchaseOrderLinesAsync(string purchaseOrderId)
        => inner.GetReturnFromPurchaseOrderLinesAsync(purchaseOrderId);

    public async Task<bool> CreateSupplierReturn(SupplierReturnDTO data)
        => await Mutate(() => inner.CreateSupplierReturn(data));

    public async Task<bool> UpdateSupplierReturn(SupplierReturnDTO data)
        => await Mutate(() => inner.UpdateSupplierReturn(data));

    public async Task<bool> SubmitSupplierReturnForApproval(SupplierReturnDTO data)
        => await Mutate(() => inner.SubmitSupplierReturnForApproval(data));

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