using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Web.BlazorServer.Components.Security;
using Web.BlazorServer.Components.Shared.CascadingValues;
using Web.BlazorServer.Services.Repositories;

namespace Web.BlazorServer.Components.Layout;

public partial class ProtectedLayout : LayoutComponentBase
{
    #region Cascading Parameters
    [CascadingParameter] HasUnsavedChangesProvider HasUnsavedChangesProvider { get; set; } = default!;
    #endregion Cascading Parameters

    #region Injects
    [Inject] AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] AppAuthenticationService AppAuthenticationService { get; set; } = default!;

    [Inject] NavigationManager NavManager { get; set; } = default!;
    [Inject] IJSRuntime? JS { get; set; } = default!;
    [Inject] IToastService ToastService { get; set; } = default!;

    //[Inject] IJSRuntime JSRuntime { get; set; } = default!;

    //[Inject] IToastService ToastService { get; set; } = default!;

    //[Inject] IInternalNavigationService InternlNavigationService { get; set; } = default!;

    //[Inject] IBreadCrumbsService BreadCrumbsService { get; set; } = default!;
    #endregion Injects

    #region Primitives
    bool Authenticating { get; set; } = true;
    string BaseUri { get; set; }
    string LogoutAPIroute { get; set; }
    //bool IsLoggedIn { get; set; } = true;
    #endregion Primitives

    #region Overrides
    protected override async Task OnInitializedAsync()
    {

        Authenticating = true;
        BaseUri = NavManager.BaseUri;
        LogoutAPIroute = "api/Authentication/logout";

        //AuthenticationState state = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        //if (state is null || (!state.User.Identity?.IsAuthenticated ?? true))
        //    NavManager.NavigateTo("/account/login?ReturnURL=dashboard", true);

        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                string returnUrl = NavManager.ToBaseRelativePath(NavManager.Uri);
                bool isLoggedIn = AppAuthenticationService.IsAuthenticated();

                if (!isLoggedIn)
                    NavManager.NavigateTo($"/?r={returnUrl}", true);
            }
            catch (Exception ex)
            {
                ToastService.Error(ex.Message);
                await PostLogout();
            }
            finally
            {
                Authenticating = false;
                await InvokeAsync(StateHasChanged);

                base.OnAfterRender(firstRender);
            }
        }
    }
    #endregion Overrides

    #region Custom Functions
    async Task PostLogout()
    {
        try
        {
            await JS!.InvokeVoidAsync("LogoutAPI", $"{BaseUri}{LogoutAPIroute}");

            NavManager!.NavigateTo("/", true);
        }
        catch (Exception ex)
        {
            ToastService.Error(ex.Message);
            NavManager.NavigateTo($"/", true);
        }
    }

    //[JSInvokable]
    //public void TriggerRefresh()
    //{
    //    NavManager!.NavigateTo(NavManager.Uri, true);
    //}

    //async Task OnBeforeInternalNavigation(LocationChangingContext locationChangingContext)
    //{
    //    if (!HasUnsavedChangesProvider.HasUnsaveChanges) return;

    //    if (!await ToastService.AlertHasUnSavedChangesAsync())
    //    {
    //        locationChangingContext.PreventNavigation();
    //        return;
    //    }
    //    else
    //    {
    //        HasUnsavedChangesProvider.HasUnsaveChanges = false;
    //        List<ViewModels.Configurations.NavigationRouteVM> breadcrumbs = BreadCrumbsService.GetBreadCrumbs(InternlNavigationService.GetNavigationRoutes(), InternlNavigationService.TargetRoute.Id);
    //        BreadCrumbsService.SetBreadcrumbs(breadcrumbs);
    //    }
    //    StateHasChanged();
    //}
    #endregion Custom Functions
}
