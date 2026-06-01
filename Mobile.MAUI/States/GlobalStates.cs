using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobile.MAUI.States;

public class GlobalStates
{
    private bool _isConnected;
    private bool _isBusy;

    public event Action Callback;
    public event Action BusyCallback;
    public string currentPage { get; set; } = string.Empty;
    public int currentPageId { get; set; }
    public bool isBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            BusyCallback?.Invoke();
        }
    }
    public bool isConnected
    {
        get => _isConnected;
        set
        {
            _isConnected = value;
            Callback?.Invoke();
        }
    }
}
