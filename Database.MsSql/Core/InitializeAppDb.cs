using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Database.MsSql.Core;

public class InitializeAppDb
{
    public static async Task InitializeAppDbAsync(IServiceProvider serviceProvider)
    {
		try
		{
			using IServiceScope Scope = serviceProvider.CreateScope();

            AppDbContext AppDbContext = Scope.ServiceProvider.GetService<AppDbContext>()
                ?? throw new Exception("AppDbContext was not registered in the services");

            ILogger<InitializeAppDb> logger = Scope.ServiceProvider
                .GetRequiredService<ILogger<InitializeAppDb>>();

            //await AppDbContext!.Database.EnsureDeletedAsync();
            await AppDbContext!.Database.EnsureCreatedAsync();
            //await AppDbSeeding.SeedStoredProcedures(AppDbContext, logger, CancellationToken.None);
            await AppDbMigration.MigrateAsync(AppDbContext);

			await AppDbSeeding.SeedData(AppDbContext, CancellationToken.None);
		}
		catch (Exception)
		{

			throw;
		}
    }
}
