using Mapster;
using Microsoft.AspNetCore.Components;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Administration.Role;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Administration.Role;
using Web.BlazorServer.ViewModels.Enums;
using KernelEnumHelper = Shared.Kernel.EnumHelper;

namespace Web.BlazorServer.Components.Pages.Administrator.Role;

public partial class RoleManagementCVU
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
    [Inject] IRoleManagementHandler RolesHandler { get; set; }
    #endregion Injects

    #region Primitives
    PageActionTypeEnum PageAction { get; set; }

    bool PasswordVisibility { get; set; } = false;
    bool Creating => PageAction == PageActionTypeEnum.Create;
    bool Updating => PageAction == PageActionTypeEnum.Update;
    bool Viewing => PageAction == PageActionTypeEnum.View;
    bool IsBusy => AppBusyService.IsBusy(ActionCreateRole) || AppBusyService.IsBusy(ActionViewRole) || AppBusyService.IsBusy(ActionUpdateRole);

    readonly string ActionCreateRole = KernelEnumHelper.GetEnumDescription(AppActions.CreateRole);
    readonly string ActionViewRole = KernelEnumHelper.GetEnumDescription(AppActions.ViewRole);
    readonly string ActionUpdateRole = KernelEnumHelper.GetEnumDescription(AppActions.UpdateRole);
    readonly string ActionGetRoles = KernelEnumHelper.GetEnumDescription(AppActions.GetAllRoles);
    #endregion Primitives

    #region Custom Classes
    RoleVM? SelectedRole { get; set; } = null;
    #endregion Custom Classes

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

        if (PageAction.Equals(PageActionTypeEnum.Create))
            FormData.Active = true;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (!Can.Do("OROL", KernelEnumHelper.GetEnumDescription(PageAction)))
            NavManager.NavigateTo("/401", true);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            await LoadDataAsync();

            await InvokeAsync(StateHasChanged);
        }
    }
    protected override async Task CancelEditing()
    {
        if (UnsavedChangesService.HasChanges)
            if (!await AlertService.HasUnsavedChangesAsync(header: "Cancel Role Update"))
                return;
        NavManager.NavigateTo($"/administration/user/role-management/view?ref={Ref}", true);
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
        NavManager.NavigateTo($"/administration/user/role-management/update?ref={Ref}", true);
    }
    #endregion Overrides

    #region Custom Functions

    async Task LoadDataAsync()
    {
        await GetRoleAsync();
    }

    async Task GetRoleAsync()
    {
        if (Viewing || Updating)
        {
            var action = await AppActionFactory.RunAsync(async () =>
            {
                AppBusyService.SetBusy(ActionViewRole, true);

                var user = await RolesHandler.GetRoleAsync(Ref);
                user.Adapt(FormData);

                AppBusyService.SetBusy(ActionViewRole, false);

            }, AppActionOptionPresets.Loading(ActionViewRole));


            await InvokeAsync(StateHasChanged);
        }
    }

    async Task Create()
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionCreateRole, true);

            var result = await RolesHandler.CreateRoleAsync(FormData);

            AppBusyService.SetBusy(ActionCreateRole, false);
            return result;

        }, AppActionOptionPresets.Confirmed(ActionCreateRole));

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
            AppBusyService.SetBusy(ActionUpdateRole, true);

            var result = await RolesHandler.UpdateRoleAsync(FormData);

            AppBusyService.SetBusy(ActionUpdateRole, false);
            return result;

        }, AppActionOptionPresets.Confirmed(ActionUpdateRole));

        action.OnSuccess(async (args) =>
        {
            UnsavedChangesService.MarkClean();
            await Return();
        });
    }

    async Task Return()
    {
        if (UnsavedChangesService.HasChanges)
            if (!await AlertService.HasUnsavedChangesAsync(header: "Cancel Role Creation"))
                return;

        NavManager.NavigateTo($"/administration/user/role-management", true);
    }

    #endregion
}
