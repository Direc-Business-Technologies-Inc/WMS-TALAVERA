namespace Web.BlazorServer.Components.Shared.CascadingValues;

public class LoadingScreenProvider
{
    private bool _isLoading;

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading == value) return;
            _isLoading = value;
            IsLoadingChanged?.Invoke(value);
        }
    }

    public event Action<bool> IsLoadingChanged;
}

