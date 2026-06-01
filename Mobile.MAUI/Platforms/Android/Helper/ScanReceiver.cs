using Android.App;
using Android.Content;
using Mobile.MAUI.Services;

namespace Mobile.MAUI.Platforms.Android.Helper;
[BroadcastReceiver(Enabled = true, Label = "data", Exported = true)]
[IntentFilter(new[] { "com.wmsmobile.appname.scan" }, Priority = (int)IntentFilterPriority.HighPriority)]
public class ScannerReceiver : BroadcastReceiver
{

    public override void OnReceive(Context context, Intent intent)
    {

        string? data = intent.GetStringExtra("com.symbol.datawedge.data_string");

        if (!string.IsNullOrWhiteSpace(data))
        {

            var scanService = MauiApplication.Current.Services.GetService<ScanService>();
            (scanService as ScanService)?.SendScannedValue(data);
        }
    }
}
