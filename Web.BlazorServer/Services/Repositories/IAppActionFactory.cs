using Shared.Utilities;

namespace Web.BlazorServer.Services.Repositories;

public interface IAppActionFactory
{
    event Func<string, Task<bool>>? ConfirmationEvent;
    Task<AppAction<T>> RunAsync<T>(Func<Task<T?>> func, AppActionFactoryOptions options);
    Task<AppAction> RunAsync(Func<Task> func, AppActionFactoryOptions options);
}
