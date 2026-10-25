using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SupportDesk.Infrastructure.Persistence;

/// <summary>
/// Used only by `dotnet ef` at design time. Runtime configuration always comes
/// from the application's configuration and environment variables.
/// </summary>
public sealed class SupportDeskDbContextFactory : IDesignTimeDbContextFactory<SupportDeskDbContext>
{
    public SupportDeskDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__SupportDesk")
            ?? "Host=localhost;Port=5432;Database=supportdesk;Username=supportdesk;Password=design-time-only";

        var options = new DbContextOptionsBuilder<SupportDeskDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(SupportDeskDbContext).Assembly.FullName))
            .Options;

        return new SupportDeskDbContext(options);
    }
}
