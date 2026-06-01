using Microsoft.EntityFrameworkCore;

namespace Database.MsSql.Core;

internal static class AppDbMigration
{
    internal static async Task MigrateAsync(AppDbContext context)
    {
        List<string> migrations = [.. (await context.Database.GetPendingMigrationsAsync(CancellationToken.None).ConfigureAwait(false))];

        if (migrations.Count != 0)
        {
            await context.Database.MigrateAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

}

