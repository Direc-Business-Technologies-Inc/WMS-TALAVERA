using Shared.Entities;
using Web.BlazorServer.ViewModels.Others;

namespace Web.BlazorServer.Handlers.Repositories.Others;

public interface IDepartmentHandler
{
    Task<(IEnumerable<DepartmentVM> Data, int Count)> GetDepartmentsListAsync(DataGridIntent intent);
}
