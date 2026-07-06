using Mobile.MAUI.Services;
using Mobile.MAUI.ViewModel;
using Shared.Libraries.ViewModel.Authentication;
using System.Text.Json;

namespace Mobile.MAUI.Components.Pages.Login;

public partial class Login
{

    [Inject] DialogService _dialogService { get; set; }

    CredentialVM Credential { get; set; } = new();

    public RadzenButton SubmitBtn { get; set; }

    AppAction<AuthenticationVM> Actionlogin { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Actionlogin = new AppAction<AuthenticationVM>
        {
            Name = "Login",
            TaskAsync = async () =>
            {
                return await Client.Post<AuthenticationVM>("/Auth/Login", Credential);
            },
            OnSuccess = async (res) =>
            {
                await AuthStateProvider.NotifyUserAuthentication(res.Data.Token);

                await SecureStorage.SetAsync("UserAuth", JsonSerializer.Serialize(res.Data));

                string? userAuth = await SecureStorage.GetAsync("UserAuth");
                if (userAuth is not null)
                {
                    var auth = JsonSerializer.Deserialize<AuthenticationVM>(userAuth);
                }

                NavManager.NavigateTo($"/", true, true);
            }
        };
    }


    async Task SubmitLogin()
    {
        await ActionFactory.ExecuteAppActionAsync(Actionlogin, showToast: true);
    }

}