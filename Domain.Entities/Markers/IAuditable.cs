namespace Domain.Markers;

public interface IAuditable<T> 
    where T : notnull
{
    public T CreatedBy { get; }
    public T UpdatedBy { get; }
    public DateTime CreatedDate { get; }
    public DateTime UpdatedDate { get; }
}
