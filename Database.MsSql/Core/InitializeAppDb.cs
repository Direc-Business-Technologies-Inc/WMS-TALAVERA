using Microsoft.Extensions.DependencyInjection;

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

			//await AppDbContext!.Database.EnsureDeletedAsync();
			//await AppDbContext!.Database.EnsureCreatedAsync();
			await AppDbMigration.MigrateAsync(AppDbContext);

			await AppDbSeeding.SeedData(AppDbContext, CancellationToken.None);
		}
		catch (Exception)
		{

			throw;
		}
    }
}
