using Api.CoreWebAPI.Controllers.Authentication.Repositories;
using Api.CoreWebAPI.Controllers.Authentication.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Api.CoreWebAPI.Registers;

public static class CoreWebApiDI
{
    public static IServiceCollection AddCoreWebApiServices(this IServiceCollection services)
    {
        services.TryAddScoped<IAuthenticationService, AuthenticationService>();

        return services;
    }
}
