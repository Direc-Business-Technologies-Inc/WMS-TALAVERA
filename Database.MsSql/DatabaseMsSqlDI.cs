using Application.UseCases.Repositories.Bases;
using Application.UseCases.Repositories.Domain.Administration.User;
using Application.UseCases.Repositories.Domain.System;
using Application.UseCases.Repositories.Domain.Transaction.InventoryCounting;
using Database.MsSql.Core;
using Database.MsSql.Implementation.Bases;
using Database.MsSql.Implementation.Reads;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Database.MsSql;

public static class DatabaseMsSqlDI
{
    public static IServiceCollection AddDatabaseMsSqlServices(this IServiceCollection services)
    {
        services.AddDbContextFactory<AppDbContext>((serviceProvier, dbOptionsBuilder) =>
        {
            string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings_AddonDB")
                                      ?? serviceProvier.GetRequiredService<IConfiguration>().GetConnectionString("AddonDB")
                                      ?? throw new Exception("Connection String for Addon Database was not set");
            
            dbOptionsBuilder.UseSqlServer(connectionString);
        });

        services.AddScoped<IAppReadRepository, AppReadRepository>();
        services.AddScoped<IAppCommandRepository, AppCommandRepository>();

        services.TryAddTransient<IDocNumReadRepo, DocNumReadRepo>();
        services.TryAddTransient<IModuleReadRepo, ModuleReadRepo>();
        services.TryAddTransient<INavigationRouteReadRepo, NavigationRouteReadRepo>();
        services.TryAddTransient<IUserReadRepo, UserReadRepo>();
        services.TryAddTransient<IRoleReadRepo, RoleReadRepo>();
        services.TryAddTransient<IInventoryCountingReadRepo, InventoryCountingReadRepo>();

        return services;
    }
}
