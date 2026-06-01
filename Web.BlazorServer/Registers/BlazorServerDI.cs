using Microsoft.Extensions.DependencyInjection.Extensions;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.Services.Implementation;
using Web.BlazorServer.Services.Repositories;

namespace Web.BlazorServer.Registers;

public static class BlazorServerDI
{
    public static IServiceCollection AddBlazorServerRegisters(this IServiceCollection services)
    {
        services.TryAddScoped<AuthorizationHelper>();
        services.TryAddScoped<UnsavedChangesService>();
        services.TryAddScoped<IAlertService, AlertService>();
        services.TryAddScoped<IAppActionFactory, AppActionFactory>();
        services.TryAddScoped<IBusyService, BusyService>();
        services.TryAddScoped<IToastService, ToastService>();
        services.TryAddScoped<IGridSettingsService, GridSettingsService>();

        return services;
    }
}
