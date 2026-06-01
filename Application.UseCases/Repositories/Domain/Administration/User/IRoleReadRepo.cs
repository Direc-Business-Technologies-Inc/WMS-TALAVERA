using Application.DataTransferObjects.Administration.Role;
using Application.DataTransferObjects.System.Modules;
using Shared.Entities;

namespace Application.UseCases.Repositories.Domain.Administration.User;

public interface IRoleReadRepo
{
    Task<(IEnumerable<RoleDTO> data, int count)> GetRoleTableDetails(DataGridIntent intent);
    Task<IEnumerable<ModulePermissionDTO>> GetRoleModulePermissions(Guid roleId);
    Task<IEnumerable<RolePermissionDTO>> GetRolePermissions(Guid roleId);
}
