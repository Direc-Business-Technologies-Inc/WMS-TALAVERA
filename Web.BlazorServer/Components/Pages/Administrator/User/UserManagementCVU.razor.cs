using Mapster;
using Microsoft.AspNetCore.Components;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Administration.Role;
using Web.BlazorServer.Handlers.Repositories.Administration.User;
using Web.BlazorServer.Handlers.Repositories.System;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Administration.Role;
using Web.BlazorServer.ViewModels.Enums;
using KernelEnumHelper = Shared.Kernel.EnumHelper;

namespace Web.BlazorServer.Components.Pages.Administrator.User;

public partial class UserManagementCVU
{
    #region Parameters
    [SupplyParameterFromQuery]
    [Parameter]
    public Guid Ref { get; set; } = Guid.NewGuid();

    [Parameter]
    public bool ModalMode { get; set; } = false;

    [Parameter]
    public PageActionTypeEnum? ModalAction { get; set; } = null;
    #endregion Parameters

    #region Injects
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;
    [Inject] IDocumentNumberHandler DocumentNumberHandler { get; set; } = default!;
    [Inject] IUserManagementHandler UsersHandler { get; set; } = default!;
    [Inject] IRoleManagementHandler RolesHandler { get; set; }
    #endregion Injects

    #region Primitives
    PageActionTypeEnum PageAction { get; set; }

    bool PasswordVisibility { get; set; } = false;
    bool Creating => PageAction == PageActionTypeEnum.Create;
    bool Updating => PageAction == PageActionTypeEnum.Update;
    bool Viewing => PageAction == PageActionTypeEnum.View;
    bool IsBusy => AppBusyService.IsBusy(ActionCreateUser) || AppBusyService.IsBusy(ActionGetUser) || AppBusyService.IsBusy(ActionUpdateUser);
    bool IsLoadingData => AppBusyService.IsBusy(ActionGetRoles) || AppBusyService.IsBusy(ActionGetUser);

    readonly string ActionCreateUser = KernelEnumHelper.GetEnumDescription(AppActions.CreateUser);
    readonly string ActionGetUser = KernelEnumHelper.GetEnumDescription(AppActions.ViewUser);
    readonly string ActionUpdateUser = KernelEnumHelper.GetEnumDescription(AppActions.UpdateUser);
    readonly string ActionGetRoles = KernelEnumHelper.GetEnumDescription(AppActions.GetAllRoles);
    #endregion Primitives

    #region Data Structures
    IEnumerable<RoleVM> Roles { get; set; } = [];
    #endregion Data Structures

    #region Overrides
    protected override void OnParametersSet()
    {
        if (!ModalMode)
            PageAction = PageActionHelper.GetPageActionType(NavManager.Uri);
        else
        {
            if (ModalMode && ModalAction != null)
                PageAction = (PageActionTypeEnum)ModalAction;
        }
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        AppBusyService.SetBusy(ActionGetUser, true);
        if (!Can.Do("OUSR", KernelEnumHelper.GetEnumDescription(PageAction)))
            NavManager.NavigateTo("/401", true);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            await LoadDataAsync();
        }
    }
    protected override async Task CancelEditing()
    {
        if (UnsavedChangesService.HasChanges)
            if (!await AlertService.HasUnsavedChangesAsync(header: "Cancel User Update"))
                return;
        NavManager.NavigateTo($"/administration/user/user-management/view?ref={Ref}", true);
    }

    protected override async Task HandleSubmit()
    {
        if (Creating)
            await Create();
        if (Updating)
            await Update();
    }

    protected override async Task InitializeEditing()
    {
        NavManager.NavigateTo($"/administration/user/user-management/update?ref={Ref}", true);
    }
    #endregion Overrides

    #region Custom Functions
    async Task ToggleVisibility()
    {
        PasswordVisibility = !PasswordVisibility;
        await InvokeAsync(StateHasChanged);
    }
    async Task LoadDataAsync()
    {
        await Task.WhenAll(
            GetRolesAsync(),
            GetUserAsync(),
            GetLatestUserSeries()
            );

        await Task.Yield();
        
        AppBusyService.SetBusy(ActionGetUser, false);
        await InvokeAsync(StateHasChanged);

    }

    async Task GetUserAsync()
    {
        if (Viewing || Updating)
        {
            var action = await AppActionFactory.RunAsync(async () =>
            {

                var user = await UsersHandler.GetUserAsync(Ref);
                user.Adapt(FormData);

                AppBusyService.SetBusy(ActionGetUser, false);

            }, AppActionOptionPresets.Loading(ActionGetUser));

        }
    }

    async Task GetRolesAsync()
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetRoles, true);

            var roles = await RolesHandler.GetAllRolesAsync(new());
            Roles = roles.Data;

        }, AppActionOptionPresets.Loading(ActionGetRoles));
    }

    async Task Create()
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionCreateUser, true);

            var result = await UsersHandler.CreateUserAsync(FormData);

            AppBusyService.SetBusy(ActionCreateUser, false);
            return result;

        }, AppActionOptionPresets.Confirmed(ActionCreateUser));

        action.OnSuccess(async (args) =>
        {
            UnsavedChangesService.MarkClean();
            await Return();
        });
    }

    async Task Update()
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionUpdateUser, true);

            var result = await UsersHandler.UpdateUserAsync(FormData);

            AppBusyService.SetBusy(ActionUpdateUser, false);
            return result;

        }, AppActionOptionPresets.Confirmed(ActionUpdateUser));

        action.OnSuccess(async (args) =>
        {
            UnsavedChangesService.MarkClean();
            await Return();
        });
    }

    async Task Return()
    {
        if (UnsavedChangesService.HasChanges)
            if (!await AlertService.HasUnsavedChangesAsync(header: "Cancel User Creation"))
                return;

        NavManager.NavigateTo($"/administration/user/user-management", true);
    }

    async Task GetLatestUserSeries()
    {
        if (!Creating)
            return;

        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetUser, true);

            var result = await DocumentNumberHandler.GetDocumentNumberAsync("User");

            AppBusyService.SetBusy(ActionGetUser, false);
            return result;

        }, AppActionOptionPresets.Loading(ActionGetUser));

        if (action.Result is not null)
            FormData.Account.UserName.Value = action.Result.NextDocNum;
    }

    #endregion
}
