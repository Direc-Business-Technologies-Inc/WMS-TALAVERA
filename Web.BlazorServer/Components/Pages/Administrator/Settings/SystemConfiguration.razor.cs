using Microsoft.AspNetCore.Components;
using Web.BlazorServer.Handlers.Repositories.Administration.Settings;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.Services.Implementation;
using Web.BlazorServer.ViewModels.Administration.Settings;

namespace Web.BlazorServer.Components.Pages.Administrator.Settings;

public partial class SystemConfiguration
{
    [Inject] ISettingsHandler settingsHandler { get; set; } = default!;

    public const string ROUTE_INDEX = "/administration/settings/system-configuration";
    public const string ACTION_GET_SETTINGS = "Get Settings";
    public List<SettingsVM> Settings = [];
    public bool IsLoadingData = true;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            IsLoadingData = true;
            await InvokeAsync(StateHasChanged);
            await LoadSettings();
            IsLoadingData = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    public async Task LoadSettings()
    {
        var action = await AppActionFactory.RunLoadingAsync(async () =>
        {
            return await settingsHandler.GetSettingsAsync();
        }, ACTION_GET_SETTINGS);

        action.OnSuccess(val =>
        {
            Settings = [.. val];
            Settings.ForEach(x => x.Dirty = false);
            return Task.CompletedTask;
        });
    }
}
