using Application.DataTransferObjects.Administration.User;
using Application.DataTransferObjects.System.Commons;
using Application.DataTransferObjects.System.Modules;
using Shared.Entities;

namespace Application.UseCases.Repositories.Domain.Administration.User;

public interface IUserReadRepo
{
    Task<(IEnumerable<UserDataGridDTO> data, int count)> GetUserTableDetails(DataGridIntent intent);
    Task<UserDataGridDTO?> GetDriverDetails(Guid driverId);
    Task<IEnumerable<ModulePermissionDTO>> GetUserModulePermissions(Guid userId);
    Task<IEnumerable<UserPermissionDTO>> GetUserPermissions(Guid userId);
    Task<IEnumerable<NavigationRouteDTO>> GetUserRoutes(Guid userId);
    Task<IEnumerable<UserDataGridDTO>> GetDriverUsers();
}
