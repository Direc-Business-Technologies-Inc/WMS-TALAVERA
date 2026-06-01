using Radzen;

namespace Web.BlazorServer.Services.Implementation;

public class UnsavedChangesService(DialogService _dialogService)
{

    bool _hasChanges = false;
    public bool HasChanges => _hasChanges;
    public void MarkDirty() => _hasChanges = true;
    public void MarkClean() => _hasChanges = false;

    public async Task NavigatingAway()
    {
        if (HasChanges)
        {
            var confirmed = await _dialogService.Confirm(
                "You have unsaved changes. Are you sure you want to discard them?",
                "Confirm Leave",
               new ConfirmOptions { OkButtonText = "Discard", CancelButtonText = "Stay" }
            );
            if (confirmed is true)
            {
                MarkClean();
                _dialogService.Close();
            }
        }
        else
        {
            _dialogService.Close();
        }

    }
    public async Task<bool> ShouldProceed()
    {
        if (HasChanges)
        {
            var confirmed = await _dialogService.Confirm(
                "You have unsaved changes. Are you sure you want to discard them?",
                "Confirm Leave",
                new ConfirmOptions { OkButtonText = "Discard", CancelButtonText = "Stay" }
            );
            if (confirmed is true)
            {
                MarkClean();
                _dialogService.Close();
                return true;
            }
            else
            {
                return false;
            }
        }

        _dialogService.Close();
        return true;
    }
}
