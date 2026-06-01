namespace Web.BlazorServer.Components.Shared.CascadingValues;

public class HasUnsavedChangesProvider
{
    bool _hasUnsaveChanges;

    public bool HasUnsaveChanges
    {
        get => _hasUnsaveChanges;
        set
        {
            if (_hasUnsaveChanges == value) return;
            _hasUnsaveChanges = value;
            HasUnsaveChangesChanged?.Invoke(value);
        }
    }
    public event Action<bool> HasUnsaveChangesChanged;
}
