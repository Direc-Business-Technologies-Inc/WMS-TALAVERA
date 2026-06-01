using Radzen;
using Web.BlazorServer.Services.Repositories;

namespace Web.BlazorServer.Services.Implementation;

public class AlertService(
    DialogService dialogService
    ) : IAlertService
{
    public async Task<bool> HasUnsavedChangesAsync(string message = "Are you sure you want to proceed?", string? header = "Unsaved Changes", string confirmText = "Yes", string cancelText = "No")
    {
        bool? decision = await dialogService.Confirm(
            message,
            header,
            new ConfirmOptions()
            {
                OkButtonText = confirmText,
                CancelButtonText = cancelText,
                ShowClose = false,
                CloseDialogOnEsc = false,
                CloseDialogOnOverlayClick = false,
                Draggable = false
            });

        return decision ?? false;
    }

    public async Task<bool> PromptAsync(string message = "Are you sure you want to proceed?", string? header = null, string confirmText = "Yes", string cancelText = "No")
    {
        bool? decision = await dialogService.Confirm(
            message,
            header,
            new ConfirmOptions()
            {
                OkButtonText = confirmText,
                CancelButtonText = cancelText,
                ShowClose = false,
                CloseDialogOnEsc = false,
                CloseDialogOnOverlayClick = false,
                Draggable = false
            });

        return decision ?? false;
    }
}
