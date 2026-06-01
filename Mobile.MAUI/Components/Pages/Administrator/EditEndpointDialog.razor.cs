using Mobile.MAUI.Services;
using Mobile.MAUI.ViewModel;
using System.Text.Json;

namespace Mobile.MAUI.Components.Pages.Administrator;

public partial class EditEndpointDialog
{

    [Inject] DialogService _dialogService { get; set; }
    [Inject] ToastifyService Toast { get; set; }
    [Inject] ApiClientService Client { get; set; }

    ClientEndpointVM EndpointVM { get; set; } = new();
    bool _hasUnsavedChanges { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await GetEndpointSettings();
    }

    async Task GetEndpointSettings()
    {
        string? settings = await SecureStorage.GetAsync("endpoint-settings");
        if (settings is not null)
        {
            EndpointVM = JsonSerializer.Deserialize<ClientEndpointVM>(settings) ?? new();
            StateHasChanged();
        }
    }

    async Task Submit()
    {
        try
        {
            await SecureStorage.SetAsync("endpoint-settings", JsonSerializer.Serialize(EndpointVM));
            Client.UpdateClient(EndpointVM);

            _dialogService.Close(true);
        }
        catch (Exception e)
        {
            await Toast.Error("Failed to update endpoint settings. Please check your settings and try again.");
        }
    }
}