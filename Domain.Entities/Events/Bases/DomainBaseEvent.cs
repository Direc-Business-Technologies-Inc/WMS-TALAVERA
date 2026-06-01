using Domain.Providers;
using MediatR;

namespace Domain.Events.Bases;

public abstract class DomainBaseEvent : INotification
{
    public DateTime TriggeredOn { get; protected set; } = DateTimeProvider.UtcNow;
}
