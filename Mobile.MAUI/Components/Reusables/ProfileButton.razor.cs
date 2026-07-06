using Mobile.MAUI.Helpers.Extensions;
using Mobile.MAUI.Services;
using Mobile.MAUI.ViewModel;
using Shared.Libraries.ViewModel;
using System.Text.Json;

namespace Mobile.MAUI.Components.Reusables;

public partial class ProfileButton
{

    SignedInUserVM User { get; set; } = new();

    AppAction<SignedInUserVM> ActionGetProfile;
    protected override async Task OnInitializedAsync()
    {
        string? settings = await SecureStorage.GetAsync("endpoint-settings");
        if (settings is not null)
        {
            var clientEndpointSettings = JsonSerializer.Deserialize<ClientEndpointVM>(settings) ?? new();
            Client.UpdateClient(clientEndpointSettings);
        }

        string userid = await AuthState.GetAuthenticatedUserId();

        ActionGetProfile = new AppAction<SignedInUserVM>
        {
            Name = "GetProfile",
            TaskAsync = async () =>
            {
                return await Client.Post<SignedInUserVM>("/User/get-profile", new { UserId = userid });
            },
            OnSuccess = async (result) =>
            {
                if (result.Data != null)
                {
                    User = result.Data;
                }
                else
                {
                    await RoleService.SetRole(null);
                    AuthStateProvider.NotifyUserLogout();
                    NavManager.NavigateTo("/login", true, true);
                }
                
                await InvokeAsync(StateHasChanged);
            }
        };

        await ActionFactory.ExecuteAppActionAsync(ActionGetProfile);
    }
    async Task ConfirmLogout()
    {
        try
        {
            var res = await Dialog.Confirm("Do you really want to logout?", "Logout confirmation");
            if (res is true)
            {
                await RoleService.SetRole(null);
                AuthStateProvider.NotifyUserLogout();
                NavManager.NavigateTo("/login", true, true);
            }
        }
        catch (Exception e)
        {

            await Toast.Error(e.Message);
        }
    }
}
