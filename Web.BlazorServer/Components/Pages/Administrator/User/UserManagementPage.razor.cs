using Microsoft.AspNetCore.Components;
using Radzen;
using Shared.Entities;
using Shared.Kernel;
using Web.BlazorServer.Components.Shared.Abstraction;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Administration.User;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Administration.User;

namespace Web.BlazorServer.Components.Pages.Administrator.User;

public partial class UserManagementPage
{
    [Inject] IUserManagementHandler UsersHandler { get; set; } = default!;
    [Inject] IGridSettingsService GridSettingsService { get; set; } = default!;

    AppDataGrid<UserDataGridVM> UsersDataGrid { get; set; }
    DataGridSettings UsersDataGridSettings { get; set; }

    string ActionGetUsers { get; } = EnumHelper.GetEnumDescription(AppActions.GetAllUsers);

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
        await GridSettingsService.SetGridSettings(UsersDataGrid.DataGrid, settings => UsersDataGridSettings = settings ?? new());
        GridSettingsLoaded = true;

        await UsersDataGrid.DataGrid.ReloadSettings();
        await UsersDataGrid.DataGrid.Reload();
    }

    async Task<DataGridResultVM<UserDataGridVM>> LoadDataAsync(DataGridIntent intent)
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionGetUsers, true);

            var response = await UsersHandler.GetAllUsersAsync(intent);

            return response;

        }, AppActionOptionPresets.Loading(ActionGetUsers));

        AppBusyService.SetBusy(ActionGetUsers, false);
        return DataGridResultVM<UserDataGridVM>.New(action.Result.data ?? [], action.Result.count);
    }

    void CreateUser() => NavManager.NavigateTo("/administration/user/user-management/create", true);
    void ViewUser(UserDataGridVM user) => NavManager.NavigateTo($"/administration/user/user-management/view?ref={user.Id}", true);
}
