using Shared.Utilities;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Services.Implementation;
using Web.BlazorServer.Services.Repositories;

namespace Web.BlazorServer.Helpers;

public static class AppActionFactoryExtensions
{
    //Task<AppAction<T>> RunAsync<T>(Func<Task<T?>> func, AppActionFactoryOptions options);

    public static async Task<AppAction<T>> RunLoadingAsync<T>(this IAppActionFactory factory, Func<Task<T?>> func, string actionName)
    {
        return await factory.RunAsync(async () =>
        {
            return await func();
        }, AppActionOptionPresets.Loading(actionName));
    }

    public static async Task<AppAction<T>> RunConfirmedAsync<T>(this IAppActionFactory factory, Func<Task<T?>> func, string actionName)
    {
        return await factory.RunAsync(async () =>
        {
            return await func();
        }, AppActionOptionPresets.Confirmed(actionName));
    }

    public static async Task<AppAction<T>> RunSilentAsync<T>(this IAppActionFactory factory, Func<Task<T?>> func, string actionName)
    {
        return await factory.RunAsync(async () =>
        {
            return await func();
        }, AppActionOptionPresets.Silent(actionName));
    }
}
