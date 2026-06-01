using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Administration.Role;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Administration.Role;

namespace Web.BlazorServer.Components.Pages.Administrator.Role;

public partial class RoleManagementPage
{
    [Inject] IRoleManagementHandler RolesHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;

    AppDataGrid<RoleVM> RolesDataGrid { get; set; }
    DataGridSettings RolesDataGridSettings { get; set; }

    string ActionGetRoles { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllRoles);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            await LoadGridSettings();
            await InvokeAsync(StateHasChanged);
        }
    }

    async Task LoadGridSettings()
    {
        await GridSettingsService.SetGridSettings(RolesDataGrid.DataGrid, settings => RolesDataGridSettings = settings ?? new());
        GridSettingsLoaded = true;

        await RolesDataGrid.DataGrid.ReloadSettings();
        await RolesDataGrid.DataGrid.Reload();
    }

    async Task<DataGridResultVM<RoleVM>> LoadDataAsync(DataGridIntent intent)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetRoles, true);

            var response = await RolesHandler.GetAllRolesAsync(intent);

            return response;

        }, AppActionOptionPresets.Loading(ActionGetRoles));

        AppBusyService.SetBusy(ActionGetRoles, false);
        return DataGridResultVM<RoleVM>.New(action.Result.Data ?? [], action.Result.Count);
    }

    void CreateRole() => NavManager.NavigateTo("/administration/user/role-management/create", true);
    void ViewRole(RoleVM user) => NavManager.NavigateTo($"/administration/user/role-management/view?ref={user.Id}", true);
}
