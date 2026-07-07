namespace Shared.Services.Repository;

public interface ICurrentUserService
{
    public Guid UserId { get; }
    public string UserName { get; }
    public int NsSubsidiaryId { get; }
    public void SetUser(Guid userId, string userName, int nsSubsidiaryId);
    public void SetUser(Guid userId, string userName);
    public void SetUser(Guid userId);
}
