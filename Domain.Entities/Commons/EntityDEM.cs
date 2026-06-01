using Ardalis.GuardClauses;
using Domain.Events.Bases;
using Domain.Markers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Commons;

public abstract class EntityDEM : IDomainEntity
{
    [Key]
    public Guid Id { get; private set; }
    [NotMapped]
    public List<DomainBaseEvent> DomainEvents { get; protected set; }

    protected EntityDEM()
    {
        Id = Guid.NewGuid();
        DomainEvents = [];
    }

    public void SetEntityId(Guid id)
    {
        Id = Guard.Against.NullOrEmpty(id, nameof(id), "Id cannot be null or empty");
    }
}
