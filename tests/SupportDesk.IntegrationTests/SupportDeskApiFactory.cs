using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace SupportDesk.IntegrationTests;

/// <summary>
/// Starts a disposable PostgreSQL container, applies real migrations, seeds the
/// database, and hosts the API in-process.
/// </summary>
public sealed class SupportDeskApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "admin@test.local";
    public const string AdminPassword = "TestAdmin!Pass123";
    public const string DemoPassword = "TestDemo!Pass123";

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("supportdesk")
        .WithUsername("supportdesk")
        .WithPassword("integration-test-password")
        .Build();

    public Task InitializeAsync() => _database.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _database.DisposeAsync();
        Dispose();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SupportDesk"] = _database.GetConnectionString(),
                ["Database:AutoMigrate"] = "true",
                ["Seed:Enabled"] = "true",
                ["Seed:AdminEmail"] = AdminEmail,
                ["Seed:AdminPassword"] = AdminPassword,
                ["Seed:DemoPassword"] = DemoPassword,
                ["Auth:RequireSecureCookies"] = "false",
                ["Security:EnableHsts"] = "false",
                ["Ai:Provider"] = "Fake",
                ["Email:Mode"] = "Logging",
                ["FileStorage:RootPath"] = Path.Combine(Path.GetTempPath(), "supportdesk-tests", Guid.NewGuid().ToString("N")),
                ["PublicUrl"] = "http://localhost",
            });
        });
    }
}
