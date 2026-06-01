#if ANDROID
using Android.Views.InputMethods;
using Android.App;
#endif

namespace Mobile.MAUI.Helpers;

public static class KeyboardHelper
{
    public static void HideKeyboard()
    {
#if ANDROID
        var activity = Platform.CurrentActivity;
        var inputMethodManager = (InputMethodManager)activity.GetSystemService(Android.Content.Context.InputMethodService);
        var currentFocus = activity.CurrentFocus;

        if (currentFocus != null)
        {
            inputMethodManager.HideSoftInputFromWindow(currentFocus.WindowToken, HideSoftInputFlags.None);
        }
#endif
    }

}
