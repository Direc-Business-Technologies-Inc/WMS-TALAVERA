using Ardalis.GuardClauses;
using Domain.Providers;

namespace Domain.Commons;

public abstract class AuditableDEM : EntityDEM
{
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedDate { get; private set; } = DateTimeProvider.UtcNow;
    public Guid? UpdatedBy { get; private set; }
    public DateTime? UpdatedDate { get; private set; }
    public bool Archived { get; private set; }
    public void Audit(Guid createdBy, DateTime createdDate, Guid? updatedBy = null, DateTime? updatedDate = null, bool archived = false)
    {
        CreatedBy = Guard.Against.NullOrEmpty(createdBy, nameof(createdBy), "Created By cannot be null or empty");
        CreatedDate = Guard.Against.NullOrOutOfSQLDateRange(createdDate, nameof(createdDate), "Created Date cannot be null or out of dates");
        if (updatedBy is not null)
            UpdatedBy = Guard.Against.NullOrEmpty(updatedBy, nameof(updatedBy), "Updated By cannot be null or empty");
        if (updatedDate is not null)
            UpdatedDate = Guard.Against.NullOrOutOfSQLDateRange(updatedDate, nameof(updatedDate), "Updated Date cannot be null or out of dates");
        Archived = Guard.Against.Null(archived, nameof(archived), "Archive status cannot be null");
    }

    public void SetCreatedBy(Guid userId)
    {
        CreatedBy = userId;
        CreatedDate = DateTimeProvider.UtcNow;
    }

    public void SetUpdatedBy(Guid userId)
    {
        UpdatedBy = userId;
        UpdatedDate = DateTimeProvider.UtcNow;
    }
    public void Archive()
    {
        Archived = true;
        UpdatedDate = DateTimeProvider.UtcNow;
    }
}
