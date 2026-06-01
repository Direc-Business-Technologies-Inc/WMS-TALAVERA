using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Shared.Kernel;
using Web.BlazorServer.Components.Base;
using Web.BlazorServer.Components.Security;
using Web.BlazorServer.Defaults;

namespace Web.BlazorServer.Components.Shared.Others;

public partial class Header : BaseComponent
{

    #region Parameters
    [Parameter]
    public bool WithAccount { get; set; } = true;

    #endregion Parameters

    #region Cascading Parameters
    #endregion Cascading Parameters

    #region Injects
    [Inject] NavigationManager NavManager { get; set; } = default!;
    [Inject] AppAuthenticationService AuthService { get; set; } = default!;
    #endregion Injects

    #region Data Structures
    #endregion Data Structures

    #region Custom Classes
    #endregion Custom Classes

    #region Primitives
    //HeaderProfileVM ProfileVM { get; set; } = new HeaderProfileVM();
    string Position { get; set; } = string.Empty;

    //string ActionLogout = EnumHelper.GetEnumDescription(ApplicationActionEnum.LogoutUser);
    //string ActionGetHeaderProfile = EnumHelper.GetEnumDescription(ApplicationActionEnum.GetHeaderProfile);
    #endregion Primitives

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {

        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {

            //JsObj = await JS.InvokeAsync<IJSObjectReference>("import", "./js/authentication.js");
            await GetHeaderProfile();

        }

    }
    async Task GetHeaderProfile()
    {
        //var action = await ActionFactory.RunAsync(async () =>
        //{
        //    ProfileVM = await AuthHandler.GetHeaderProfile(await AuthStateTask.GetSignedInUserId());
        //    await InvokeAsync(StateHasChanged);
        //},
        //AppActionOptionPresets.ForFetching(ActionGetHeaderProfile));
    }
    #region Custom Functions
    void NavigateToHome()
    {
        NavManager.NavigateTo("/", true);
    }

    async Task Logout()
    {
        await JS!.InvokeVoidAsync("LogoutAPI", $"{NavManager.BaseUri}api/Authentication/logout");

        NavManager.NavigateTo("/", true);
    }
    #endregion Custom Functions
}
