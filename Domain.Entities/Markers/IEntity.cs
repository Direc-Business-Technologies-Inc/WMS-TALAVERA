using Domain.Events.Bases;

namespace Domain.Markers;

public interface IEntity : IEntity<Guid>
{
    public List<DomainBaseEvent> DomainEvents { get; }
}

public interface IEntity<T> : IMarker
{
    public T Id { get; }
}
