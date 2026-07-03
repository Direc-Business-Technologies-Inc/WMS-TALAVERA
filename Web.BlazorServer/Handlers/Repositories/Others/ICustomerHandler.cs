using Shared.Entities;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Handlers.Repositories.Others;

public interface ICustomerHandler
{
    Task<(IEnumerable<CustomerVM>, int)> GetCustomersListAsync(DataGridIntent intent);
}
