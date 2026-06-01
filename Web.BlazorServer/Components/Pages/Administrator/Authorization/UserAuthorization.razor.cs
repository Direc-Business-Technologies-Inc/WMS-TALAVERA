using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using Shared.Kernel;
using System.Data;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Administration.Authorization;
using Web.BlazorServer.Handlers.Repositories.Administration.Role;
using Web.BlazorServer.Handlers.Repositories.Administration.User;
using Web.BlazorServer.Handlers.Repositories.System;
using Web.BlazorServer.Services.Implementation;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Administration.User;
using Web.BlazorServer.ViewModels.System;

namespace Web.BlazorServer.Components.Pages.Administrator.Authorization;

public partial class UserAuthorization
{
    #region Parameters
    [Parameter] public AuthorizationType CurrentTab { get; set; } = AuthorizationType.User;
    [Parameter] public EventCallback<AuthorizationType> CurrentTabChanged { get; set; }
    #endregion Parameters

    #region Injects
    [Inject] IUserManagementHandler UserManagementHandler { get; set; } = default!;
    [Inject] IRoleManagementHandler RoleManagementHandler { get; set; } = default!;
    [Inject] IModuleHandler ModuleHandler { get; set; } = default!;
    [Inject] IAuthorizationHandler AuthorizationHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;
    #endregion Injects

    #region Primitives
    string ActionGetUsers { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllRoles);
    string ActionGetUserPermissions { get; } = EnumHelper.GetEnumDescription(AppActions.GetUserPermissions);
    string ActionUpdateUserPermissions { get; } = EnumHelper.GetEnumDescription(AppActions.UpdateUserPermissions);
    string ActionGetModules { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllModules);
    bool IsEditingPermissions { get; set; } = false;
    bool Editable => SelectedUser is not null && IsEditingPermissions;
    #endregion Primitives

    #region Custom Classes
    IDataGridIntentAdapter DatagridAdapter { get; set; } = default!;
    UserDataGridVM? SelectedUser { get; set; } = null;
    #endregion Custom Classes

    #region Data Structures
    RadzenListBox<UserDataGridVM> UserListBox { get; set; } = default!;
    DataGridResultVM<UserDataGridVM> Users { get; set; } = new DataGridResultVM<UserDataGridVM>();
    DataGridResultVM<ModuleDataGridVM> Modules { get; set; } = new DataGridResultVM<ModuleDataGridVM>();
    List<UserPermissionVM> UserPermissions { get; set; } = [];
    List<UserPermissionVM> UserPermissionsChanges { get; set; } = [];
    #endregion Data Structures

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
    async Task UserLoadData(LoadDataArgs args)
    {

        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetUsers, true);

            DatagridAdapter = new DataGridIntentAdapter(args);

            if (args.Filter is not null)
                args.Filters = [new FilterDescriptor()
            {
                Property = UserListBox.TextProperty,
                Type = typeof(string),
                FilterOperator = FilterOperator.Contains,
                FilterValue = args.Filter,

            }];

            DatagridAdapter.AdaptToMyFilterQueries();
            DatagridAdapter.AdaptToMySortQueries();
            DatagridAdapter.AdaptToPagination();

            var response = await UserManagementHandler.GetAllUsersAsync(DatagridAdapter.QueryIntent);
            Users.Items = response.data;
            Users.Count = response.count;

            AppBusyService.SetBusy(ActionGetUsers, false);

        }, AppActionOptionPresets.Loading(ActionGetUsers));

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

    async Task UserPermissionsLoadDataAsync()
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetUserPermissions, true);
            await InvokeAsync(StateHasChanged);

            if (SelectedUser is null)
                return;

            IEnumerable<UserPermissionVM> response = await AuthorizationHandler.GetUserPermissions(SelectedUser.Id);
            UserPermissions = (List<UserPermissionVM>)response;

        }, AppActionOptionPresets.Loading(ActionGetModules));

        AppBusyService.SetBusy(ActionGetUserPermissions, false);
    }

    async Task OnUserSelect()
    {
        UserPermissions.Clear();
        await UserPermissionsLoadDataAsync();
        await Task.Delay(500);
        await InvokeAsync(StateHasChanged);
    }

    bool HasPermission(string permission, string moduleCode)
    {
        bool hasChanges = UserPermissionsChanges.Any(rp =>
            rp.Permission.ModuleCode == moduleCode &&
            rp.Permission.Permission == permission);

        bool hasPermission = UserPermissions.Any(rp =>
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

        if (UserPermissionsChanges.Any(rp => rp.Permission.Id == module.Id))
        {
            UserPermissionsChanges.RemoveAll(rp => rp.Permission.Id == module.Id);
        }
        else
        {
            UserPermissionsChanges.Add(new UserPermissionVM
            {
                UserId = SelectedUser!.Id,
                Permission = module
            });
        }
    }

    IEnumerable<UserPermissionVM> MergeChanges()
    {
        List<UserPermissionVM> MergedChanges = UserPermissions;

        foreach (var change in UserPermissionsChanges)
        {
            List<UserPermissionVM> existing = [.. UserPermissions.Where(x => x.UserId == SelectedUser!.Id
                                                                             && x.Permission.ModuleCode == change.Permission.ModuleCode
                                                                             && x.Permission.Permission == change.Permission.Permission)];
            if (existing.Count <= 0)
                MergedChanges.Add(change);
            else
                MergedChanges.RemoveAll(x => x.UserId == SelectedUser!.Id
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
        UserPermissionsChanges.Clear();

        await InvokeAsync(StateHasChanged);
    }

    async Task SaveChanges()
    {

        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionUpdateUserPermissions, true);

            var result = AuthorizationHandler.UpdateUserPermissions(MergeChanges());

            AppBusyService.SetBusy(ActionUpdateUserPermissions, false);
            return result;

        }, AppActionOptionPresets.Confirmed(ActionUpdateUserPermissions));

        action.OnSuccess(async (args) =>
        {
            UnsavedChangesService.MarkClean();
            UserPermissionsChanges.Clear();
            IsEditingPermissions = false;
            await Task.Delay(500);
            await InvokeAsync(StateHasChanged);
        });
    }

    #endregion Custom Functions
}

