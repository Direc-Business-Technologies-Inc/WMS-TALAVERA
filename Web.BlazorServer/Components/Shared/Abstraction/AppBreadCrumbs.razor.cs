using Microsoft.AspNetCore.Components;
using Shared.Kernel;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.System;
using Web.BlazorServer.ViewModels.Enums;
using Web.BlazorServer.ViewModels.System;

namespace Web.BlazorServer.Components.Shared.Abstraction;

public partial class AppBreadCrumbs
{
    #region Parameters
    [Parameter] public required string ModuleCode { get; set; } = string.Empty;
    [Parameter] public PageActionTypeEnum? PageAction { get; set; } = null;
    [Parameter] public List<NavigationRouteVM> AdditionalRoutes { get; set; } = [];
    #endregion Parameters

    #region Injects
    [Inject] INavigationRouteHandler NavigationRouteHandler { get; set; } = null!;
    #endregion Injects

    #region Primitives
    string ActionGetNavigationRoutes => EnumHelper.GetEnumDescription(AppActions.GetNavigationRoutes);
    bool IsBusy => AppBusyService.IsBusy(ActionGetNavigationRoutes);

    #endregion Primitives

    #region Data Sctructures
    IEnumerable<NavigationRouteVM> Routes { get; set; } = [];
    #endregion Data Sctructures

    #region Overrides

    protected override void OnInitialized()
    {
        base.OnInitialized();
        AppBusyService.SetBusy(ActionGetNavigationRoutes, true);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            var action = await AppActionFactory.RunAsync(async () =>
            {

                var response = await NavigationRouteHandler.GetModuleNavigationRoutesAsync(ModuleCode);

                AppBusyService.SetBusy(ActionGetNavigationRoutes, false);
                return response;

            }, AppActionOptionPresets.Loading(ActionGetNavigationRoutes));

            if (action.IsSuccess && action.Result is not null)
            {
                Routes = [.. action.Result, .. AdditionalRoutes];
                await InvokeAsync(StateHasChanged);
            }

        }
    }

    string ActionIcon() => PageAction switch
    {
        PageActionTypeEnum.Create => "add_circle",
        PageActionTypeEnum.Update => "edit",
        _ => string.Empty
    };

    #endregion Overrides
}
