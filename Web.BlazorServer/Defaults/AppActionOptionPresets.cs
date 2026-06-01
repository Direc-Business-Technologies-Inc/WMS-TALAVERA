using Shared.Utilities;

namespace Web.BlazorServer.Defaults;

public class AppActionOptionPresets
{
    public static AppActionFactoryOptions Loading(string actionName) => new()
    {
        ActionName = actionName,
        ShowLoadingIndicator = true,
        ShowConfirmationDialog = false,
        ShowToastOnSuccess = false,
        ShowToastOnFailure = true
    };

    public static AppActionFactoryOptions Confirmed(string actionName) => new()
    {
        ActionName = actionName,
        ShowLoadingIndicator = true,
        ShowConfirmationDialog = true,
        ShowToastOnSuccess = true,
        ShowToastOnFailure = true
    };

    public static AppActionFactoryOptions Silent(string actionName) => new()
    {
        ActionName = actionName,
        ShowLoadingIndicator = true,
        ShowConfirmationDialog = false,
        ShowToastOnSuccess = true,
        ShowToastOnFailure = true
    };
}
