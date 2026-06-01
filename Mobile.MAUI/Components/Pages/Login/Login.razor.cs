using Mobile.MAUI.Services;
using Mobile.MAUI.ViewModel;

namespace Mobile.MAUI.Components.Pages.Login;

public partial class Login
{

    [Inject] DialogService _dialogService { get; set; }

    CredentialVM Credential { get; set; } = new();

    public RadzenButton SubmitBtn { get; set; }

    AppAction<string> Actionlogin { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Actionlogin = new AppAction<string>
        {
            Name = "Login",
            TaskAsync = async () =>
            {
                return await Client.Post<string>("/Auth/Login", Credential);
            },
            OnSuccess = async (res) =>
            {
                await AuthStateProvider.NotifyUserAuthentication(res.Data);
                NavManager.NavigateTo($"/", true, true);
            }
        };
    }


    async Task SubmitLogin()
    {
        await ActionFactory.ExecuteAppActionAsync(Actionlogin, showToast: true);
    }

}