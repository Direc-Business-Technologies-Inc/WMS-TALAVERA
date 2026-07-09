using Application.DataTransferObjects.Transactions.SupplierReturn;
using Shared.Entities;
using Web.BlazorServer.ViewModels.Transaction.Receiving;
using Web.BlazorServer.ViewModels.Transaction.SupplierReturn;

namespace Web.BlazorServer.Handlers.Repositories.Transaction.SupplierReturn;

public interface ISupplierReturnHandler
{
    Task<(IEnumerable<SupplierReturnDataGridVM> Data, int Count)> GetReturnsDataGridAsync(DataGridIntent intent);
    Task<(IEnumerable<PurchaseOrderDataGridVM> Data, int Count)> GetPurchaseOrdersDataGridAsync(DataGridIntent intent);
    Task<(IEnumerable<ReturnCategoryVM> Data, int Count)> GetReturnCategories(DataGridIntent intent);
    Task<(IEnumerable<ReturnStatusVM> Data, int Count)> GetReturnStatuses(DataGridIntent intent);
    Task<SupplierReturnVM?> GetReturnAsync(string Ref);
    Task<IEnumerable<SupplierReturnLineVM>> GetReturnLinesAsync(string Ref);
    Task<SupplierReturnVM?> GetReturnFromPurchaseOrderAsync(string Ref);
    Task<IEnumerable<SupplierReturnLineVM>> GetReturnFromPurchaseOrderLinesAsync(string Ref);
    Task<bool> CreateSupplierReturnAsync(SupplierReturnVM data);
    Task<(IEnumerable<PurchaseCategoryVM> Data, int Count)> GetPurchaseCategoriesAsync(DataGridIntent intent);
    Task<(IEnumerable<PurchaseSubcategoryVM> Data, int Count)> GetPurchaseSubCategoriesAsync(PurchaseCategoryVM category, DataGridIntent intent);

}
