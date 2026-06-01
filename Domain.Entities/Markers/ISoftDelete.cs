namespace Domain.Markers;

public interface ISoftDelete<T> : IMarker
    where T : notnull
{
    public bool SoftDeleted { get; }
    public T SoftDeletedBy { get; }
    public DateTime? SoftDeletedDate { get; }
}
