using Shared.Services.Repository;

namespace Web.BlazorServer.Services.Implementation;

public class CurrentUserService : ICurrentUserService
{
    Guid _userId = default!;
    string _userName = string.Empty;
    int _nsSubsidiaryId;
    public Guid UserId => _userId;
    public string UserName => _userName;
    public int NsSubsidiaryId => _nsSubsidiaryId;

    public void SetUser(Guid userId, string userName, int nsSubsidiaryId)
    {
        _userId = userId;
        _userName = userName;
        _nsSubsidiaryId = nsSubsidiaryId;
    }

    public void SetUser(Guid userId, string userName)
    {
        _userId = userId;
        _userName = userName;
    }

    public void SetUser(Guid userId) => _userId = userId;
}
