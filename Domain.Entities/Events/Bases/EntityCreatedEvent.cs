using Domain.Markers;

namespace Domain.Events.Bases;

public class EntityCreatedEvent<T>(T entity) : DomainBaseEvent
    where T : IEntity
{
    public T Entity { get; } = entity;
}
