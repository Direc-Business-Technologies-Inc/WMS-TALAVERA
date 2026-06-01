using Web.BlazorServer.ViewModels.Administration.Role;
using Web.BlazorServer.ViewModels.Administration.User;

namespace Web.BlazorServer.Handlers.Repositories.Administration.Authorization;

public interface IAuthorizationHandler
{
    Task<bool> UpdateUserPermissions(IEnumerable<UserPermissionVM> permissions);
    Task<bool> UpdateRolePermissions(IEnumerable<RolePermissionVM> permissions);
    Task<bool> CascadeRolePermissions(IEnumerable<RolePermissionVM> permissions);
    Task<IEnumerable<RolePermissionVM>> GetRolePermissions(Guid roleId);
    Task<IEnumerable<UserPermissionVM>> GetUserPermissions(Guid userId);
}
