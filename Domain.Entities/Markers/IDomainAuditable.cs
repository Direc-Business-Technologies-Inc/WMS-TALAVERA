namespace Domain.Markers;

public interface IDomainAuditable : IAuditable<Guid>
{
    void Audit(
        Guid createdBy,
        Guid updatedBy,
        DateTime createdDate,
        DateTime updateDate
    );
}
