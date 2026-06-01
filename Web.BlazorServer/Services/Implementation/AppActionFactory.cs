using Shared.Utilities;
using Web.BlazorServer.Services.Repositories;

namespace Web.BlazorServer.Services.Implementation;

public class AppActionFactory(
    IToastService ToastService,
    IBusyService BusyService
    ) : IAppActionFactory
{
    public event Func<string, Task<bool>>? ConfirmationEvent;

    public async Task<AppAction<T>> RunAsync<T>(Func<Task<T?>> func, AppActionFactoryOptions options)
    {
        var action = new AppAction<T> { Options = options };

        try
        {
            if (string.IsNullOrWhiteSpace(action.Options.ActionName))
                throw new ArgumentException("ActionName must be provided in AppActionFactoryOptions.");

            if (options.ShowConfirmationDialog)
            {
                if (ConfirmationEvent is not null)
                {
                    bool confirmed = await ConfirmationEvent.Invoke(options.ActionName);
                    if (confirmed is not true)
                    {
                        action.IsCancelled = true;
                        return action;
                    }
                }
            }

            if (options.ShowLoadingIndicator)
                BusyService.SetBusy(options.ActionName, true);

            #region Auditable Action

            #endregion

            action.SetResult(await func());

            if(options.ShowToastOnSuccess)
                ToastService.Success($"{options.ActionName} completed successfully.");

        }
        catch (Exception ex)
        {
            action.SetException(ex);

            if(options.ShowToastOnFailure)
                ToastService.Error($"{options.ActionName} failed: {ex.Message}");
        }
        finally
        {
            if(options.ShowLoadingIndicator)
                BusyService.SetBusy(options.ActionName, false);
        }

        return action;
    }

    public async Task<AppAction> RunAsync(Func<Task> func, AppActionFactoryOptions options)
    {
        var action = new AppAction { Options = options };

        try
        {
            if (string.IsNullOrWhiteSpace(action.Options.ActionName))
                throw new ArgumentException("ActionName must be provided in AppActionFactoryOptions.");

            if (options.ShowConfirmationDialog)
            {
                if (ConfirmationEvent is not null)
                {
                    bool confirmed = await ConfirmationEvent.Invoke(options.ActionName);
                    if (confirmed is not true)
                    {
                        action.IsCancelled = true;
                        return action;
                    }
                }
            }

            if (options.ShowLoadingIndicator)
                BusyService.SetBusy(options.ActionName, true);

            #region Auditable Action

            #endregion

            await func();

            if (options.ShowToastOnSuccess)
                ToastService.Success($"{options.ActionName} completed successfully.");

        }
        catch (Exception ex)
        {
            action.SetException(ex);

            if (options.ShowToastOnFailure)
                ToastService.Error($"{options.ActionName} failed: {ex.Message}");
        }
        finally
        {
            if (options.ShowLoadingIndicator)
                BusyService.SetBusy(options.ActionName, false);
        }

        return action;
    }
}
