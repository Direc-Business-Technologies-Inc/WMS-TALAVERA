using Shared.Entities;
using Web.BlazorServer.ViewModels.Administration.Role;
using Web.BlazorServer.ViewModels.Administration.User;

namespace Web.BlazorServer.Handlers.Repositories.Administration.Role;

public interface IRoleManagementHandler
{
    Task<RoleVM?> GetRoleAsync(Guid reference);
    Task<(IEnumerable<RoleVM> Data, int Count)> GetAllRolesAsync(DataGridIntent intent);
    Task<bool> CreateRoleAsync(RoleVM role);
    Task<bool> UpdateRoleAsync(RoleVM role);
    Task<IEnumerable<RoleVM>> GetTopRolesAsync();
    Task<IEnumerable<RoleVM>> GetRolesByNameAsync(string searchTerm);
}
