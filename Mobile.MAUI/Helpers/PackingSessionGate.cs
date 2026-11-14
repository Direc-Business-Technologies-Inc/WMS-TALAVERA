namespace Mobile.MAUI.Helpers;

/// <summary>
/// Keeps a save/refresh/vendor change exclusive with scans and row edits,
/// including barcode lookups which have not completed yet. Operations are never
/// queued: the caller tells the operator to wait and retry instead.
/// </summary>
public sealed class PackingSessionGate
{
    private readonly object _sync = new();
    private int _edits;
    private bool _exclusive;

    public bool IsBusy
    {
        get { lock (_sync) return _exclusive; }
    }

    public IDisposable? TryBeginEdit()
    {
        lock (_sync)
        {
            if (_exclusive) return null;
            _edits++;
            return new Lease(() => { lock (_sync) _edits--; });
        }
    }

    public IDisposable? TryBeginOperation()
    {
        lock (_sync)
        {
            if (_exclusive || _edits > 0) return null;
            _exclusive = true;
            return new Lease(() => { lock (_sync) _exclusive = false; });
        }
    }

    private sealed class Lease(Action release) : IDisposable
    {
        private Action? _release = release;
        public void Dispose() => Interlocked.Exchange(ref _release, null)?.Invoke();
    }
}
