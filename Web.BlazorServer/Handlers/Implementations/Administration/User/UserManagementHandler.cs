using Application.DataTransferObjects.Administration.User;
using Application.UseCases.Commands.Administration.User;
using Application.UseCases.Commands.Transaction.Administration.User;
using Application.UseCases.Queries.Administration.User;
using Mapster;
using MediatR;
using Shared.Entities;
using Web.BlazorServer.Handlers.Repositories.Administration.User;
using Web.BlazorServer.ViewModels.Administration.User;

namespace Web.BlazorServer.Handlers.Implementations.Administration.User;

public class UserManagementHandler(
    ISender Sender) 
    : IUserManagementHandler
{
    public async Task<bool> CreateUserAsync(UserVM user)
    {
        var dto = user.Adapt<UserDTO>();

        CreateUserCmd cmd = new(dto);
        await Sender.Send(cmd);

        return true;
    }

    public async Task<(IEnumerable<UserDataGridVM> data, int count)> GetAllUsersAsync(DataGridIntent intent)
    {
        GetAllUsersQry qry = new(intent);
        var dto = await Sender.Send(qry);

        return (dto.Data.Adapt<IEnumerable<UserDataGridVM>>(), dto.Count);
    }

    public async Task<UserVM?> GetUserAsync(Guid reference)
    {
        GetUserQry qry = new(reference);
        var response = await Sender.Send(qry);

        return response.Adapt<UserVM>();
    }

    public async Task<bool> UpdateUserAsync(UserVM user)
    {
        var dto = user.Adapt<UserDTO>();

        UpdateUserCmd cmd = new(dto);
        await Sender.Send(cmd);

        return true;
    }

    public async Task<bool> UpdateUserPasswordAsync(UserPasswordVM user)
    {
        var dto = user.Adapt<UserPasswordDTO>();

        UpdateUserPasswordCmd cmd = new(dto);
        await Sender.Send(cmd);

        return true;
    }
}
