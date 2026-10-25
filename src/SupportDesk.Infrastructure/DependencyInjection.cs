using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SupportDesk.Application.Abstractions;
using SupportDesk.Infrastructure.Ai;
using SupportDesk.Infrastructure.Email;
using SupportDesk.Infrastructure.Identity;
using SupportDesk.Infrastructure.Persistence;
using SupportDesk.Infrastructure.Seed;
using SupportDesk.Infrastructure.Services;
using SupportDesk.Infrastructure.Storage;

namespace SupportDesk.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuration is resolved when the context is created so late-bound
        // providers (test hosts, secret stores) are honored.
        services.AddDbContext<SupportDeskDbContext>((provider, options) =>
        {
            var runtimeConfiguration = provider.GetRequiredService<IConfiguration>();
            var connectionString = runtimeConfiguration.GetConnectionString("SupportDesk")
                ?? throw new InvalidOperationException("ConnectionStrings:SupportDesk is required.");

            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(SupportDeskDbContext).Assembly.FullName));
        });

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<SupportDeskDbContext>());
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IUserDirectory, UserDirectory>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<DataSeeder>();

        services.Configure<FileStorageOptions>(configuration.GetSection("FileStorage"));
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        services.Configure<SeedOptions>(configuration.GetSection("Seed"));
        services.Configure<AiOptions>(configuration.GetSection("Ai"));
        services.Configure<EmailOptions>(configuration.GetSection("Email"));
        services.Configure<SupportDesk.Application.Services.PublicUrlOptions>(options =>
            options.Url = configuration.GetValue<string>("PublicUrl") ?? "http://localhost:8080");

        var emailMode = configuration.GetValue<string>("Email:Mode") ?? "Logging";
        if (string.Equals(emailMode, "Smtp", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            services.AddScoped<IEmailSender, LoggingEmailSender>();
        }

        services.AddHttpClient("ai");
        var aiProvider = configuration.GetValue<string>("Ai:Provider") ?? "Fake";
        if (string.Equals(aiProvider, "OpenAi", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IAiProvider, OpenAiCompatibleAiProvider>();
        }
        else
        {
            services.AddScoped<IAiProvider, FakeAiProvider>();
        }

        return services;
    }

    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services)
    {
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 12;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = true;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<SupportDeskDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        return services;
    }
}
