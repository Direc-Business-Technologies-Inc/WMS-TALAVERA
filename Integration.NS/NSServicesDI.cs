using Application.UseCases.Repositories.Integration.Others;
using Integration.NS.Implementations;
using Integration.NS.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Integration.NS;

public static class NSServicesDI
{
    public static IServiceCollection AddNSServicesIntegraton(this IServiceCollection services)
    {

        services.AddScoped<INetSuiteApiClientService, NetSuiteApiClientService>();


        services.AddScoped<HttpContextAccessor>();

        services.AddNSImplementationsIntegraton();

        return services;
    }
}
