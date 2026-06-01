namespace Shared.Services.Repository;

public interface ICurrentUserService
{
    public Guid UserId { get; }
    public string UserName { get; }
    public void SetUser(Guid userId, string userName);
    public void SetUser(Guid userId);
}
