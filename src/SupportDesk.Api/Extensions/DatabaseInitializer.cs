using Microsoft.EntityFrameworkCore;
using SupportDesk.Infrastructure.Persistence;
using SupportDesk.Infrastructure.Seed;

namespace SupportDesk.Api.Extensions;

public static class DatabaseInitializer
{
    /// <summary>
    /// Applies migrations only when explicitly enabled, then seeds required
    /// permissions, roles, the initial administrator, and demo data.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var configuration = app.Configuration;
        var dbContext = scope.ServiceProvider.GetRequiredService<SupportDeskDbContext>();

        if (configuration.GetValue("Database:AutoMigrate", false))
        {
            await dbContext.Database.MigrateAsync();
        }

        var seedEnabled = configuration.GetValue("Seed:Enabled", true);
        if (!seedEnabled)
        {
            return;
        }

        var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
        await seeder.SeedAsync(CancellationToken.None);
    }
}
