using Application.DataTransferObjects.Transactions.SupplierReturn;
using Shared.Entities;
using Web.BlazorServer.ViewModels.Transaction.SupplierReturn;

namespace Web.BlazorServer.Handlers.Repositories.Transaction.SupplierReturn;

public interface ISupplierReturnHandler
{
    Task<(IEnumerable<SupplierReturnDataGridVM> Data, int Count)> GetReturnsDataGridAsync(DataGridIntent intent);
    Task<(IEnumerable<ReturnCategoryVM> Data, int Count)> GetReturnCategories(DataGridIntent intent);
    Task<(IEnumerable<ReturnStatusVM> Data, int Count)> GetReturnStatuses(DataGridIntent intent);
    Task<SupplierReturnVM?> GetReturnAsync(string Ref);
    Task<IEnumerable<SupplierReturnLineVM>> GetReturnLinesAsync(string Ref);
    Task<bool> CreateSupplierReturnAsync(SupplierReturnVM data);
}
