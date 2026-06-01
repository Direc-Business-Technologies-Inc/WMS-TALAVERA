using Mobile.MAUI.Interfaces;
using Mobile.MAUI.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Mobile.MAUI.Services;

public class InternetConnectivity : IInternetConnectivity
{
    public event EventHandler<ConnectivityChangedEventArgs> ConnectivityChanged;
    private GlobalStates _globalState { get; set; }

    public InternetConnectivity(GlobalStates globalState)
    {
        _globalState = globalState;
        Connectivity.ConnectivityChanged += HandleInternetConnectivity;
    }

    ~InternetConnectivity() => Connectivity.ConnectivityChanged -= HandleInternetConnectivity;

    private async Task<bool> PingTest()
    {
#if ANDROID
                Java.Lang.Process p1 = Java.Lang.Runtime.GetRuntime().Exec("ping -c 1 google.com");
                int returnVal = p1.WaitFor();
                return returnVal == 1;
#endif

        Ping pingSender = new();
        PingReply reply = await pingSender.SendPingAsync("8.8.8.8");

        return reply.Status == IPStatus.Success;
    }

    private async void HandleInternetConnectivity(object sender, ConnectivityChangedEventArgs e)
    {
        await RefreshConnection();
        ConnectivityChanged?.Invoke(sender, e);
    }

    public async Task RefreshConnection()
    {
        //bool pingTest = await PingTest();
        _globalState.isConnected = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
    }

}
