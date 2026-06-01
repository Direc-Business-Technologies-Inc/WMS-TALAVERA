using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using Shared.Kernel;
using Sprache;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Administration.Authorization;
using Web.BlazorServer.Handlers.Repositories.Administration.Role;
using Web.BlazorServer.Handlers.Repositories.System;
using Web.BlazorServer.Services.Implementation;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Administration.Role;
using Web.BlazorServer.ViewModels.System;

namespace Web.BlazorServer.Components.Pages.Administrator.Authorization;

public partial class RoleAuthorization
{
    #region Parameters
    [Parameter] public AuthorizationType CurrentTab { get; set; } = AuthorizationType.Role;
    [Parameter] public EventCallback<AuthorizationType> CurrentTabChanged { get; set; }
    #endregion Parameters

    #region Injects
    [Inject] IRoleManagementHandler RoleManagementHandler { get; set; } = default!;
    [Inject] IModuleHandler ModuleHandler { get; set; } = default!;
    [Inject] IAuthorizationHandler AuthorizationHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;
    #endregion Injects

    #region Primitives
    string ActionGetRoles { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllRoles);
    string ActionGetRolePermissions { get; } = EnumHelper.GetEnumDescription(AppActions.GetRolePermissions);
    string ActionUpdateRolePermissions { get; } = EnumHelper.GetEnumDescription(AppActions.UpdateRolePermissions);
    string ActionCascadeRolePermissions { get; } = EnumHelper.GetEnumDescription(AppActions.CascadeRolePermissions);
    string ActionGetModules { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllModules);
    bool IsEditingPermissions { get; set; } = false;
    bool Editable => SelectedRole is not null && IsEditingPermissions;
    #endregion Primitives

    #region Data Structures
    RadzenListBox<RoleVM> RolesListBox { get; set; } = default!;
    DataGridResultVM<RoleVM> Roles { get; set; } = new DataGridResultVM<RoleVM>();
    DataGridResultVM<ModuleDataGridVM> Modules { get; set; } = new DataGridResultVM<ModuleDataGridVM>();
    List<RolePermissionVM> RolePermissions { get; set; } = [];
    List<RolePermissionVM> RolePermissionsChanges { get; set; } = [];
    #endregion Data Structures

    #region Custom Classes
    IDataGridIntentAdapter DatagridAdapter { get; set; } = default!;
    RoleVM? SelectedRole { get; set; } = null;
    #endregion Custom Classes

    #region Overrides
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            await ModuleLoadDataAsync();
        }
    }
    #endregion Overrides

    #region Custom Functions

    async Task RoleLoadData(LoadDataArgs args)
    {

        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetRoles, true);

            DatagridAdapter = new DataGridIntentAdapter(args);

            if (args.Filter is not null)
                args.Filters = [new FilterDescriptor()
            {
                Property = RolesListBox.TextProperty,
                Type = typeof(string),
                FilterOperator = FilterOperator.Contains,
                FilterValue = args.Filter,

            }];

            DatagridAdapter.AdaptToMyFilterQueries();
            DatagridAdapter.AdaptToMySortQueries();
            DatagridAdapter.AdaptToPagination();

            var response = await RoleManagementHandler.GetAllRolesAsync(DatagridAdapter.QueryIntent);
            Roles.Items = response.Data;
            Roles.Count = response.Count;

            AppBusyService.SetBusy(ActionGetRoles, false);

        }, AppActionOptionPresets.Loading(ActionGetRoles));

    }

    async Task ModuleLoadDataAsync()
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetModules, true);

            var response = await ModuleHandler.GetModuleTableDetailsAsync(new()
            {
                Take = 999,
                Skip = 0
            });
            Modules = DataGridResultVM<ModuleDataGridVM>.New(response.Data, response.Count);

        }, AppActionOptionPresets.Loading(ActionGetModules));

        AppBusyService.SetBusy(ActionGetModules, false);
    }

    async Task RolePermissionsLoadDataAsync()
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetRolePermissions, true);
            await InvokeAsync(StateHasChanged);

            if (SelectedRole is null)
                return;

            IEnumerable<RolePermissionVM> response = await AuthorizationHandler.GetRolePermissions(SelectedRole.Id);
            RolePermissions = (List<RolePermissionVM>)response;

        }, AppActionOptionPresets.Loading(ActionGetModules));

        AppBusyService.SetBusy(ActionGetRolePermissions, false);
    }

    async Task OnRoleSelect()
    {
        RolePermissions.Clear();
        await RolePermissionsLoadDataAsync();
        await Task.Delay(500);
        await InvokeAsync(StateHasChanged);
    }

    bool HasPermission(string permission, string moduleCode)
    {
        bool hasChanges = RolePermissionsChanges.Any(rp =>
            rp.Permission.ModuleCode == moduleCode &&
            rp.Permission.Permission == permission);

        bool hasPermission = RolePermissions.Any(rp =>
            rp.Permission.ModuleCode == moduleCode &&
            rp.Permission.Permission == permission);

        if (hasChanges && hasPermission)
            return false;
        else if (hasChanges && !hasPermission)
            return true;
        else
            return hasPermission;
    }

    void OnPermissionChange(ModulePermissionVM? module)
    {
        if (module is null)
            return;

        UnsavedChangesService.MarkDirty();

        if (RolePermissionsChanges.Any(rp => rp.Permission.Id == module.Id))
        {
            RolePermissionsChanges.RemoveAll(rp => rp.Permission.Id == module.Id);
        }
        else
        {
            RolePermissionsChanges.Add(new RolePermissionVM
            {
                RoleId = SelectedRole!.Id,
                Permission = module
            });
        }
    }

    IEnumerable<RolePermissionVM> MergeChanges()
    {
        List<RolePermissionVM> MergedChanges = RolePermissions;

        foreach (var change in RolePermissionsChanges)
        {
            List<RolePermissionVM> existing = [.. RolePermissions.Where(x => x.RoleId == SelectedRole!.Id
                                                                             && x.Permission.ModuleCode == change.Permission.ModuleCode
                                                                             && x.Permission.Permission == change.Permission.Permission)];
            if (existing.Count <= 0)
                MergedChanges.Add(change);
            else
                MergedChanges.RemoveAll(x => x.RoleId == SelectedRole!.Id
                                             && x.Permission.ModuleCode == change.Permission.ModuleCode
                                             && x.Permission.Permission == change.Permission.Permission);
        }

        return MergedChanges;
    }

    async Task OnEdit()
    {
        IsEditingPermissions = true;
        await InvokeAsync(StateHasChanged);
    }

    async Task CancelEdit()
    {
        if (UnsavedChangesService.HasChanges)
        {
            if (!await AlertService.HasUnsavedChangesAsync())
                return;

            UnsavedChangesService.MarkClean();
        }

        IsEditingPermissions = false;
        RolePermissionsChanges.Clear();

        await InvokeAsync(StateHasChanged);
    }

    async Task SaveChanges()
    {

        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionUpdateRolePermissions, true);

            var result = AuthorizationHandler.UpdateRolePermissions(MergeChanges());

            AppBusyService.SetBusy(ActionUpdateRolePermissions, false);
            return result;

        }, AppActionOptionPresets.Confirmed(ActionUpdateRolePermissions));

        action.OnSuccess(async (args) =>
        {
            UnsavedChangesService.MarkClean();
            RolePermissionsChanges.Clear();
            IsEditingPermissions = false;
            await Task.Delay(500);
            await InvokeAsync(StateHasChanged);
        });
    }

    async Task CascadeChanges()
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionCascadeRolePermissions, true);

            var result = AuthorizationHandler.CascadeRolePermissions(MergeChanges());

            AppBusyService.SetBusy(ActionCascadeRolePermissions, false);
            return result;

        }, AppActionOptionPresets.Confirmed(ActionCascadeRolePermissions));

        action.OnSuccess(async (args) =>
        {
            UnsavedChangesService.MarkClean();
            RolePermissionsChanges.Clear();
            IsEditingPermissions = false;
            await Task.Delay(500);
            await InvokeAsync(StateHasChanged);
        });
    }

    #endregion Custom Functions

}
