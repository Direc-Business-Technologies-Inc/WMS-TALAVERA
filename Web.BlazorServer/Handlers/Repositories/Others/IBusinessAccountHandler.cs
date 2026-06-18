using Shared.Entities;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Handlers.Repositories.Others;

public interface IBusinessAccountHandler
{
    Task<(IEnumerable<BusinessAccountVM> Data, int Count)> GetBusinessAccountsDataGridAsync(DataGridIntent intent);
    Task<(IEnumerable<BusinessAccountVM> Data, int Count)> GetBusinessAccountsBySubsidiaryDataGridAsync(DataGridIntent intent, int subsidiary);
}
