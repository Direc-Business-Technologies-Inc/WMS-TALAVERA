using Radzen;
using Shared.Libraries.Entities;

namespace Mobile.MAUI.Services;

public class ActionFactoryService(DialogService Dialog, ToastifyService Toast)
{

    public async Task<AppAction<T>> ExecuteAppActionAsync<T>(AppAction<T> appAction, bool confirm = false, bool showToast = false)
    {
        try
        {
            if (confirm)
            {
                var res = await Dialog.Confirm(
                    "Are you sure you want to proceed?",
                    "Confirm Action",
                    new ConfirmOptions
                    {
                        OkButtonText = "Proceed",
                        CancelButtonText = "Cancel"
                    });

                if (res is not true) return appAction;
            }

            appAction.Busy = true;
            appAction.Result = await appAction.TaskAsync();
            appAction.Busy = false;



            if (appAction.OnSuccess != null && appAction.Result.Success)
            {
                await appAction.OnSuccess(appAction.Result);
            }

            if (appAction.OnFailure != null && !appAction.Result.Success)
            {
                await appAction.OnFailure(appAction.Result);
            }

            if (showToast && appAction.Result != null)
            {
                if (appAction.Result.Success)
                {

                    await Toast.Success($"{appAction.Name} completed successfully.");
                }
                else
                {
                    await Toast.Error($"Action failed: {appAction.Result.ErrorMessage}");
                }
            }
            return appAction;
        }
        catch (Exception e)
        {

            await Toast.Error(e.Message);
        }
        finally
        {
            appAction.Busy = false;
        }

        return appAction;
    }
    public async Task<AppAction> ExecuteAppActionAsync(AppAction appAction, bool confirm = false, bool showToast = false)
    {
        try
        {
            if (confirm)
            {
                var res = await Dialog.Confirm(
                    "Are you sure you want to proceed?",
                    "Confirm Action",
                    new ConfirmOptions
                    {
                        OkButtonText = "Proceed",
                        CancelButtonText = "Cancel"
                    });

                if (res is not true) return appAction;
            }
            appAction.Busy = true;
            appAction.Result = await appAction.TaskAsync();
            appAction.Busy = false;



            if (appAction.OnSuccess != null && appAction.Result.Success)
            {
                await appAction.OnSuccess(appAction.Result);
            }

            if (appAction.OnFailure != null && !appAction.Result.Success)
            {
                await appAction.OnFailure(appAction.Result);
            }

            if (showToast && appAction.Result != null)
            {
                if (appAction.Result.Success)
                {

                    await Toast.Success($"{appAction.Name} completed successfully.");
                }
                else
                {
                    await Toast.Error($"Action failed: {appAction.Result.ErrorMessage}");
                }
            }
            return appAction;


        }
        catch (Exception e)
        {

            await Toast.Error(e.Message);
        }
        finally
        {
            appAction.Busy = false;
        }
        return appAction;
    }
}


public class AppAction<T>
{
    public string Name { get; set; } = string.Empty;
    public bool Busy { get; set; }
    public ApiResult<T> Result { get; set; }
    public required Func<Task<ApiResult<T>>> TaskAsync { get; init; }
    public Func<ApiResult<T>, Task>? OnSuccess { get; set; }
    public Func<ApiResult<T>, Task>? OnFailure { get; set; }
}
public class AppAction
{
    public string Name { get; set; } = string.Empty;
    public bool Busy { get; set; }
    public ApiResult Result { get; set; }
    public required Func<Task<ApiResult>> TaskAsync { get; init; }

    public Func<ApiResult, Task>? OnSuccess { get; set; }
    public Func<ApiResult, Task>? OnFailure { get; set; }
}
