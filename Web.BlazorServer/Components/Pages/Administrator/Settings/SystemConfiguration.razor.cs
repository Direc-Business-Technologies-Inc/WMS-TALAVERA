using Microsoft.AspNetCore.Components;
using Microsoft.Identity.Client;
using Web.BlazorServer.Components.Security;
using Web.BlazorServer.Handlers.Repositories.Administration.Settings;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.Services.Implementation;
using Web.BlazorServer.ViewModels.Administration.Settings;

namespace Web.BlazorServer.Components.Pages.Administrator.Settings;

public partial class SystemConfiguration
{
    [Inject] ISettingsHandler settingsHandler { get; set; } = default!;
    [Inject] AppAuthenticationService authService { get; set; } = default!;

    public const string ROUTE_INDEX = "/administration/settings/system-configuration";
    public const string ACTION_GET_SETTINGS = "Get Settings";
    public List<SettingsVM> Settings = [];
    public bool IsLoadingData = true;
    public bool IsSavingData = false;
    public bool CanUpdate = false;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            CanUpdate = authService.HasPermission("OSYS.UPDATE");
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
            Settings.ForEach(x => x.IsDirty = false);
            return Task.CompletedTask;
        });
    }

    public void ResetSettings()
    {
        Settings.ForEach(x => x.Reset());
    }

    public async Task TrySaveSettings()
    {
        if (!Settings.Any(x => x.IsDirty))
        {
            ToastService.Warning("No setting changed");
            return;
        }

        IsSavingData = true;
        await InvokeAsync(StateHasChanged);

        List<Task<(string code, bool success)>> saveTasks = 
            [.. Settings.Where(x => x.IsDirty).Select(x =>
                settingsHandler.SetSettingAsync(x.Code, x.Value)
            )];

        var results = await Task.WhenAll(saveTasks);
        var successes = results.Where(x => x.success);
        var fails = results.Where(x => !x.success); // iterates through the collection twice
        // group by doesnt work like java collector
        // why

        foreach (var item in successes)
        {
            var setting = Settings.FirstOrDefault(x => x.Code.Equals(item.code));
            if (setting is not null) setting.IsDirty = false;
        }

        if (fails.Any()) 
        {
            var failedSettings = fails.Select(x => Settings.FirstOrDefault(y => y.Code.Equals(x.code)));
            ToastService.Error($"Failed to save settings {string.Join(", ", failedSettings.Where(x => x is not null).Select(x => x!.Title))}");
        }
        else
        {
            ToastService.Success("Settings saved successfully");
        }

        IsSavingData = false;
        await InvokeAsync(StateHasChanged);
    }
}
