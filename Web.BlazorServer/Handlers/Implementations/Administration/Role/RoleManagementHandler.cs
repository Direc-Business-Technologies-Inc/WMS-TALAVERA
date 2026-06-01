using Application.DataTransferObjects.Administration.Role;
using Application.UseCases.Commands.Administration.Role;
using Application.UseCases.Queries.Administration.Role;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Administration.Role;
using Web.BlazorServer.ViewModels.Administration.Role;

namespace Web.BlazorServer.Handlers.Implementations.Administration.Role;

public class RoleManagementHandler(
    ISender Sender)
    : IRoleManagementHandler
{
    public async Task<bool> CreateRoleAsync(RoleVM role)
    {
        var dto = role.Adapt<RoleDTO>();

        CreateRoleCmd cmd = new(dto);
        await Sender.Send(cmd);

        return true;
    }

    public async Task<(IEnumerable<RoleVM> Data, int Count)> GetAllRolesAsync(DataGridIntent intent)
    {
        GetAllRolesQry qry = new(intent);
        var dto = await Sender.Send(qry);

        return (dto.Data.Adapt<IEnumerable<RoleVM>>(), dto.Count);
    }

    public async Task<RoleVM?> GetRoleAsync(Guid reference)
    {
        GetRoleQry qry = new(reference);
        var response = await Sender.Send(qry);

        return response.Adapt<RoleVM>();
    }

    public async Task<IEnumerable<RoleVM>> GetRolesByNameAsync(string searchTerm)
    {
        GetRolesByNameQry qry = new(searchTerm);
        var response = await Sender.Send(qry);

        return response.Adapt<IEnumerable<RoleVM>>();
    }

    public async Task<IEnumerable<RoleVM>> GetTopRolesAsync()
    {
        GetTopRolesQry qry = new();
        var response = await Sender.Send(qry);

        return response.Adapt<IEnumerable<RoleVM>>();
    }

    public async Task<bool> UpdateRoleAsync(RoleVM role)
    {
        var dto = role.Adapt<RoleDTO>();

        UpdateRoleCmd cmd = new(dto);
        await Sender.Send(cmd);

        return true;
    }
}
