using Microsoft.AspNetCore.Components;
using Web.BlazorServer.Components.Shared.CascadingValues;
using Web.BlazorServer.Services.Repositories;

namespace Web.BlazorServer.Components;

public partial class Routes
{
    [Inject] IAppActionFactory AppActionFactory { get; set; } = default!;
    [Inject] IAlertService AlertService { get; set; } = default!;

    HasUnsavedChangesProvider HasUnsaveChangesProvider { get; set; } = new();
    LoadingScreenProvider LoadingScreenProvider { get; set; } = new();

    protected override void OnInitialized()
    {
        AppActionFactory.ConfirmationEvent += ShowPromptAsync;
    }

    async Task<bool> ShowPromptAsync(string action)
    {
        bool decision =  await AlertService.PromptAsync(
            $"Are you sure you want to proceed with action: {action}?",
            "Confirmation",
            "Proceed",
            "Cancel");

        return decision is true;
    }
}
