using Application.DataTransferObjects.Administration.Role;
using Application.DataTransferObjects.Administration.User;
using Application.UseCases.Commands.Administration.Role;
using Application.UseCases.Commands.Administration.User;
using Application.UseCases.Queries.Administration.Role;
using Application.UseCases.Queries.Administration.User;
using Mapster;
using MediatR;
using Web.BlazorServer.Handlers.Repositories.Administration.Authorization;
using Web.BlazorServer.ViewModels.Administration.Role;
using Web.BlazorServer.ViewModels.Administration.User;

namespace Web.BlazorServer.Handlers.Implementations.Administration.Authorization;

public class AuthorizationHandler(
    ISender Sender) 
    : IAuthorizationHandler
{
    public async Task<bool> CascadeRolePermissions(IEnumerable<RolePermissionVM> permissions)
    {
        CascadeRolePermissionsCmd cmd = new(permissions.Adapt<IEnumerable<RolePermissionDTO>>());
        var response = await Sender.Send(cmd);
        return response;
    }

    public async Task<IEnumerable<RolePermissionVM>> GetRolePermissions(Guid roleId)
    {
        GetRolePermissionsQry query = new(roleId);
        IEnumerable<RolePermissionDTO> response = await Sender.Send(query);

        return response.Adapt<IEnumerable<RolePermissionVM>>();
    }

    public async Task<IEnumerable<UserPermissionVM>> GetUserPermissions(Guid userId)
    {
        GetUserPermissionsQry query = new(userId);
        IEnumerable<UserPermissionDTO> response = await Sender.Send(query);
        return response.Adapt<IEnumerable<UserPermissionVM>>();
    }

    public async Task<bool> UpdateRolePermissions(IEnumerable<RolePermissionVM> permissions)
    {
        UpdateRolePermissionsCmd cmd = new(permissions.Adapt<IEnumerable<RolePermissionDTO>>());
        var response = await Sender.Send(cmd);
        return response;
    }

    public async Task<bool> UpdateUserPermissions(IEnumerable<UserPermissionVM> permissions)
    {
        UpdateUserPermissionsCmd cmd = new(permissions.Adapt<IEnumerable<UserPermissionDTO>>());
        var response = await Sender.Send(cmd);
        return response;
    }
}
