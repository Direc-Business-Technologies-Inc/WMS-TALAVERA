using Mapster;
using MapsterMapper;
using Mobile.MAUI.Helpers.Others;
using Mobile.MAUI.Interfaces;

namespace Mobile.MAUI.Services;

public static class Extensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddSingleton<UnsaveChangesHandlerService>();
        services.AddSingleton<IInternetConnectivity, InternetConnectivity>();
        services.AddScoped<ScanService>();
        services.AddScoped<ToastifyService>();
        //services.AddScoped<ScannerLogicService>();
        services.AddScoped<ActionFactoryService>();

        services.AddSingleton<RoleService>();
        services.AddSingleton<ApiClientService>();
        services.AddSingleton<PrinterApiClientService>();
        #region Maspter
        var config = new TypeAdapterConfig();
        MapsterConfig.RegisterMappings(config);
        services.AddSingleton<IMapper>(new Mapper(config));
        #endregion Maspter

        return services;
    }
}
