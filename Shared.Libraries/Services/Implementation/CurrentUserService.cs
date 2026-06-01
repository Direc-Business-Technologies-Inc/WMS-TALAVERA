using Shared.Services.Repository;

namespace Web.BlazorServer.Services.Implementation;

public class CurrentUserService : ICurrentUserService
{
    Guid _userId = default!;
    string _userName = string.Empty;
    public Guid UserId => _userId;
    public string UserName => _userName;

    public void SetUser(Guid userId, string userName)
    {
        _userId = userId;
        _userName = userName;
    }

    public void SetUser(Guid userId) => _userId = userId;
}
