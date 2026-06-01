using Microsoft.AspNetCore.Components;
using Shared.Kernel;
using Shared.Utilities;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Administration.User;
using Web.BlazorServer.Services.Implementation;
using Web.BlazorServer.ViewModels.Administration.User;

namespace Web.BlazorServer.Components.Pages.Administrator.User;

public partial class UserPasswordManagement
{
    #region Parameters
    [Parameter] public required UserDataGridVM User { get; set; }
    #endregion Parameters

    #region Injects
    [Inject] IUserManagementHandler UsersHandler { get; set; } = default!;
    #endregion Injects

    #region Primitives
    string ActionUpdatePassword => EnumHelper.GetEnumDescription(AppActions.UpdateUser);
    bool IsBusy => AppBusyService.IsBusy(ActionUpdatePassword);
    bool PasswordVisibility { get; set; } = false;
    bool ConfirmPasswordVisibility { get; set; } = false;
    #endregion Primitives

    #region Overrides

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        FormData.UserId = User.Id;
        FormData.Password = string.Empty;
        FormData.ConfirmPassword = string.Empty;
    }
    protected override async Task CancelEditing()
    {
        DialogService.Close(false);
    }

    protected override async Task HandleSubmit()
    {
        var action = await AppActionFactory.RunAsync(async () =>
        {
            AppBusyService.SetBusy(ActionUpdatePassword, true);

            var result = await UsersHandler.UpdateUserPasswordAsync(FormData);

            AppBusyService.SetBusy(ActionUpdatePassword, false);
            return result;

        }, AppActionOptionPresets.Confirmed(ActionUpdatePassword));

        action.OnSuccess(async (args) =>
        {
            UnsavedChangesService.MarkClean();
            DialogService.Close(true);
        });

    }

    protected override Task InitializeEditing()
    {
        throw new NotImplementedException();
    }
    #endregion Overrides

    #region Custom Functions
    async Task ToggleVisibility()
    {
        PasswordVisibility = !PasswordVisibility;
        await InvokeAsync(StateHasChanged);
    }

    async Task ToggleConfirmVisibility()
    {
        ConfirmPasswordVisibility = !ConfirmPasswordVisibility;
        await InvokeAsync(StateHasChanged);
    }

    #endregion Custom Functions
}
