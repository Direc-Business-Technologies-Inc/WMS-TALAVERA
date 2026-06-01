using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mobile.MAUI.Interfaces;

public interface IInternetConnectivity
{
    public event EventHandler<ConnectivityChangedEventArgs> ConnectivityChanged;
    public Task RefreshConnection();
}
