using Microsoft.AspNetCore.Components;
using Mobile.MAUI.Services;

namespace Mobile.MAUI.Components.Reusables;
public partial class Confirmable
{
    [Inject] NavigationManager _navManager { get; set; }
    [Inject] DialogService _dialogService { get; set; }
    [Inject] UnsaveChangesHandlerService _unsaveChangesHandler { get; set; }
    [Parameter]
    public string? Path { get; set; }

    public async Task TryLeave(string? path = null)
    {
        if (_unsaveChangesHandler.HasUnsavedChanges)
        {
            var confirm = await _dialogService.Confirm("You have unsaved changes. Are you sure you want to go back?", "Leave confirmation");
            if (confirm is true)
            {
                _unsaveChangesHandler.MarkAsClean();
                if (path != null)
                {
                    _navManager.NavigateTo(path);
                }
                else
                {

                    _navManager.Refresh(true);
                }

            }
        }
        else
        {
            _navManager.NavigateTo(path);
        }

    }
    public async Task TryLeaveDialog()
    {
        if (_unsaveChangesHandler.HasUnsavedChanges)
        {
            var confirm = await _dialogService.Confirm("You have unsaved changes. Are you sure you want to go back?", "Leave confirmation");
            if (confirm is true)
            {
                _unsaveChangesHandler.MarkAsClean();
                _dialogService.Close();
            }
        }
    }
}
