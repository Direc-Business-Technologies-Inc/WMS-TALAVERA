using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Database.MsSql.Core;

/// <summary>Lets EF scaffold migrations without starting the API or a running Blazor Server.</summary>
public sealed class AppDbContextDesignFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings_AddonDB");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Set ConnectionStrings_AddonDB to scaffold the database migration.");

        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connection).Options);
    }
}
