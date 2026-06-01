using Database.Libraries.DataAccess;
using Database.Libraries.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Database.Libraries;

public static class DatabaseLibrariesDI
{
    public static IServiceCollection AddDatabaseLibrariesServices(this IServiceCollection services)
    {
        services.TryAddSingleton<ISqlQueryManager, SqlQueryManager>();

        return services;
    }
}
