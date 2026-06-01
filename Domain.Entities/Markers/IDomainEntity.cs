namespace Domain.Markers;

public interface IDomainEntity : IEntity
{
    void SetEntityId(Guid id);
}
