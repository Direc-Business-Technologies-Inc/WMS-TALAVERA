using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Services.Repository;
using Web.BlazorServer.Services.Implementation;

namespace Shared.Services;

public static class SharedServicesDI
{
    public static IServiceCollection AddSharedServices(this IServiceCollection services)
    {

        services.TryAddScoped<ICurrentUserService,CurrentUserService>();

        return services;
    }
}
