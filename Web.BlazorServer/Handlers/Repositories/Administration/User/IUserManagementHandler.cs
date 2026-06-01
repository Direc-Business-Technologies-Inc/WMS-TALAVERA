using Shared.Entities;
using Web.BlazorServer.ViewModels.Administration.User;

namespace Web.BlazorServer.Handlers.Repositories.Administration.User;

public interface IUserManagementHandler
{
    Task<UserVM?> GetUserAsync(Guid reference);
    Task<(IEnumerable<UserDataGridVM> data, int count)> GetAllUsersAsync(DataGridIntent intent);
    Task<bool> CreateUserAsync(UserVM user);
    Task<bool> UpdateUserAsync(UserVM user);
    Task<bool> UpdateUserPasswordAsync(UserPasswordVM user);
}
