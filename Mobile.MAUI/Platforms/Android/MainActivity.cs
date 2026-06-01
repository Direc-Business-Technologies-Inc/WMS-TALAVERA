using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Mobile.MAUI.Platforms.Android;

namespace Mobile.MAUI
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        TestBroadcastReceiver _testReceiver;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            _testReceiver = new TestBroadcastReceiver();
            // Set the app to full-screen mode
            Window.DecorView.SystemUiFlags = (
                SystemUiFlags.Fullscreen |
                SystemUiFlags.HideNavigation |
                SystemUiFlags.ImmersiveSticky);
            // For Android 13+, use RegisterReceiver with flags
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Tiramisu)
            {
                void RegisterReceivers()
                {
                    IntentFilter filter = new IntentFilter();
                    filter.AddCategory("android.intent.category.DEFAULT");
                    filter.AddAction("com.ndzl.DW");
                    filter.AddAction("com.symbol.datawedge.api.RESULT_ACTION");
                    filter.AddAction("com.wmsmobile.appname.scan");
                    Intent regres = AndroidX.Core.Content.ContextCompat.RegisterReceiver(this, _testReceiver, filter, AndroidX.Core.Content.ContextCompat.ReceiverExported);
                }

                RegisterReceivers();
            }
            else
            {
                // For older Android versions
                RegisterReceiver(_testReceiver, new Android.Content.IntentFilter("com.symbol.datawedge.scanner_status"));
            }
            // Ensure keyboard resizing works
            Window.SetSoftInputMode(SoftInput.AdjustResize);

            base.OnCreate(savedInstanceState);
        }
    }
}
