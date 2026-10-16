using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Mobile.MAUI.Helpers.Extensions;
using Mobile.MAUI.Services;
using Mobile.MAUI.ViewModel;
using Radzen;
using System.Text.Json;

namespace Mobile.MAUI.Components.Layout.MainLayout;

public partial class MainLayout : IDisposable
{
    [Inject] ApiClientService Client { get; set; }
    [Inject] JWTAuthStateProvider AuthStateProvider { get; set; }
    [Inject] NavigationManager NavManager { get; set; }
    [Inject] DialogService Dialog { get; set; }
    [Inject] RoleService RoleService { get; set; }
    [Inject] UnsaveChangesHandlerService UnsaveChangesHandler { get; set; }

    [CascadingParameter]
    Task<AuthenticationState> AuthState { get; set; }


    protected override async Task OnInitializedAsync()
    {
        Client.Unauthorized += HandleUnauthorizedAsync;

        string userId = await AuthState.GetAuthenticatedUserId();
        string? settings = await SecureStorage.GetAsync("endpoint-settings");
        if (settings is not null)
        {
            var clientEndpointSettings = JsonSerializer.Deserialize<ClientEndpointVM>(settings) ?? new();
            Client.UpdateClient(clientEndpointSettings);
        }
    }

    private Task HandleUnauthorizedAsync()
    {
        return InvokeAsync(HandleUnauthorizedCoreAsync);
    }

    private async Task HandleUnauthorizedCoreAsync()
    {
        var message = UnsaveChangesHandler.HasUnsavedChanges
            ? "Your session has expired and your unsaved scans could not be saved. Please log in again."
            : "Your session has expired. Please log in again.";

        await RoleService.SetRole(null);
        AuthStateProvider.NotifyUserLogout();
        UnsaveChangesHandler.MarkAsClean();

        try
        {
            await Dialog.Alert(message, "Session expired");
        }
        catch
        {
            // A dialog failure must not prevent returning to the login screen.
        }

        NavManager.NavigateTo("/login", false, true);
    }

    public void Dispose()
    {
        Client.Unauthorized -= HandleUnauthorizedAsync;
    }

}
