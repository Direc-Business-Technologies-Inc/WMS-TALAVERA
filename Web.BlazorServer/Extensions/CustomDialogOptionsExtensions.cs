using Radzen;

namespace Web.BlazorServer.Extensions;

public static class CustomDialogOptionsExtensions
{
    public static DialogOptions FullScreenMode(this DialogOptions dialogOptions)
    {
        dialogOptions.Width = "100%";
        dialogOptions.Height = "100%";

        return dialogOptions;
    }

    public static DialogOptions MaximizedMode(this DialogOptions dialogOptions)
    {
        dialogOptions.Width = "80%";
        dialogOptions.Height = "80%";

        return dialogOptions;
    }

    public static DialogOptions WideTableMode(this DialogOptions dialogOptions)
    {
        dialogOptions.Width = "80%";
        dialogOptions.Height = "60%";

        return dialogOptions;
    }

    public static DialogOptions MediumModalMode(this DialogOptions dialogOptions)
    {
        dialogOptions.Width = "400px";

        return dialogOptions;
    }

    public static DialogOptions HideCloseButton(this DialogOptions dialogOptions)
    {
        dialogOptions.ShowClose = false;
        

        return dialogOptions;
    }

    public static DialogOptions CloseByCustomButton(this DialogOptions dialogOptions)
    {
        dialogOptions.ShowClose = false;
        dialogOptions.CloseDialogOnEsc = false;
        dialogOptions.CloseDialogOnOverlayClick = false;

        return dialogOptions;
    }
}
