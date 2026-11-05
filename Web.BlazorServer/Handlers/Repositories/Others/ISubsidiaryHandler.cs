using Shared.Entities;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Handlers.Repositories.Others;

public interface ISubsidiaryHandler
{
    Task<(IEnumerable<SubsidiaryVM> Data, int Count)> GetSubsidiariesAsync(DataGridIntent intent, CancellationToken ct = default);
    Task<(IEnumerable<SubsidiaryVM> Data, int Count)> GetCurrentUserSubsidiariesAsync(DataGridIntent intent, CancellationToken ct = default);
    Task<(IEnumerable<SubsidiaryVM> Data, int Count)> GetSubsidiariesByVendorAsync(DataGridIntent intent, int vendorId, CancellationToken ct = default);
    Task<(IEnumerable<SubsidiaryVM> Data, int Count)> GetSubsidiariesByCustomerAsync(DataGridIntent intent, int customerId, CancellationToken ct = default);
    Task<(IEnumerable<PurchaseCategoryVM> Data, int Count)> GetPurchaseCategoriesAsync(DataGridIntent intent, CancellationToken ct = default);
    Task<(IEnumerable<PurchaseSubcategoryVM> Data, int Count)> GetPurchaseSubCategoriesAsync(PurchaseCategoryVM category, DataGridIntent intent, CancellationToken ct = default);
    Task<SubsidiaryVM?> GetSubsidiaryAsync(int id, CancellationToken ct = default);
}
