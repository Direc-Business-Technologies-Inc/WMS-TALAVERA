namespace Domain.Markers;

public interface IActivateable : IMarker
{
    public bool Active { get; }
}
