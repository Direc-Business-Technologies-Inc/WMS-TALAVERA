
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Web.BlazorServer.Components.Security;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.ViewModels.Security;
using KernelEnumHelper = Shared.Kernel.EnumHelper;

namespace Web.BlazorServer.Components.Pages;

public partial class Index
{
    #region Parameters
    [Parameter][SupplyParameterFromQuery] public string? r { get; set; }
    #endregion Parameters

    #region Injects
    [Inject] AppAuthenticationService AuthenticationService { get; set; }
    [Inject] HttpClient HttpClient { get; set; }
    #endregion Injects

    #region Primitives
    bool IsBusy => AppBusyService.IsBusy(ActionLogin);
    readonly string ActionLogin = KernelEnumHelper.GetEnumDescription(AppActions.Login);

    string Message { get; set; } = string.Empty;
    bool? Success { get; set; } = null;
    string BaseUri { get; set; } = string.Empty;
    bool PasswordVisible { get; set; } = false;
    bool Authenticated { get; set; } = false;
    bool ShowMessage { get; set; } = true;
    #endregion Primitives

    #region Overrides
    protected override void OnInitialized()
    {
        base.OnInitialized();

        Authenticated = AuthenticationService.IsAuthenticated();

        if (Authenticated)
            NavManager.NavigateTo("/dashboard", true);
    }

    protected override async Task CancelEditing()
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            if (!AppBusyService.IsBusy(ActionLogin))
                AppBusyService.SetBusy(ActionLogin, true);

            var response = await JS!.InvokeAsync<AuthenticationVM>("LogoutAPI", FormData.UserName, FormData.Password, $"{NavManager.BaseUri}api/Authentication");
            ToastService.Error("User Id cannot be parsed. Please try again.");

            AppBusyService.SetBusy(ActionLogin, false);
        }, AppActionOptionPresets.Loading(ActionLogin));
    }

    protected override async Task HandleSubmit()
    {

        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionLogin, true);

            var response = await JS!.InvokeAsync<AuthenticationVM>("LoginAPI", FormData.UserName, FormData.Password, $"{NavManager.BaseUri}api/Authentication");
            Success = response.Success;
            ShowMessage = true;
            if (!response.Success)
                Message = response.Message ?? "Something wrong happenned. Please try again.";
            else if (!string.IsNullOrEmpty(r))
                NavManager!.NavigateTo($"/{r}", true);
            else
                NavManager!.NavigateTo("/dashboard", true);

        }, AppActionOptionPresets.Loading(ActionLogin));
    }

    protected override Task InitializeEditing()
    {
        throw new NotImplementedException();
    }



    #endregion Overrides

    #region Custom Functions
    void ChangePasswordVisiblity() => PasswordVisible = !PasswordVisible;

    void HideMessage() => ShowMessage = false;
    #endregion Custom Functions
}
