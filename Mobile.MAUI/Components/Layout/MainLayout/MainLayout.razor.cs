using Microsoft.AspNetCore.Components;
using Mobile.MAUI.Helpers.Extensions;
using Mobile.MAUI.Services;
using Mobile.MAUI.ViewModel;
using System.Text.Json;

namespace Mobile.MAUI.Components.Layout.MainLayout;

public partial class MainLayout
{
    [Inject] ApiClientService Client { get; set; }

    [CascadingParameter]
    Task<AuthenticationState> AuthState { get; set; }


    protected override async Task OnInitializedAsync()
    {
        string userId = await AuthState.GetAuthenticatedUserId();
        string? settings = await SecureStorage.GetAsync("endpoint-settings");
        if (settings is not null)
        {
            var clientEndpointSettings = JsonSerializer.Deserialize<ClientEndpointVM>(settings) ?? new();
            Client.UpdateClient(clientEndpointSettings);
        }
    }

}
