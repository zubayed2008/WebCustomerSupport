using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Domain.Permissions;
using SupportDesk.Infrastructure.Identity;

namespace SupportDesk.Infrastructure.Seed;

public sealed partial class DataSeeder
{
    private async Task SeedAdministratorAsync(CancellationToken cancellationToken)
    {
        var adminRole = await _roleManager.FindByNameAsync(SystemRoles.Administrator)
            ?? throw new InvalidOperationException("Administrator role was not created.");

        var hasAdmin = await _dbContext.UserRoles.AnyAsync(x => x.RoleId == adminRole.Id, cancellationToken);
        if (hasAdmin)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.AdminEmail) || string.IsNullOrWhiteSpace(_options.AdminPassword))
        {
            throw new InvalidOperationException("Seed:AdminEmail and Seed:AdminPassword are required to create the initial administrator.");
        }

        var email = _options.AdminEmail.Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = "System Administrator",
                IsActive = true,
                CreatedAt = _clock.UtcNow,
                UpdatedAt = _clock.UtcNow,
            };

            EnsureSuccess(await _userManager.CreateAsync(user, _options.AdminPassword));
        }

        EnsureSuccess(await _userManager.AddToRoleAsync(user, SystemRoles.Administrator));
    }

    private async Task SeedDemoUsersAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.DemoPassword))
        {
            return;
        }

        var demoUsers = new (string Email, string DisplayName, string Role)[]
        {
            ("manager@example.com", "Morgan Manager", SystemRoles.Manager),
            ("agent@example.com", "Alex Agent", SystemRoles.Agent),
            ("customer@example.com", "Casey Customer", SystemRoles.Customer),
        };

        foreach (var (email, displayName, role) in demoUsers)
        {
            if (await _userManager.FindByEmailAsync(email) is not null)
            {
                continue;
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = displayName,
                IsActive = true,
                CreatedAt = _clock.UtcNow,
                UpdatedAt = _clock.UtcNow,
            };

            EnsureSuccess(await _userManager.CreateAsync(user, _options.DemoPassword));
            EnsureSuccess(await _userManager.AddToRoleAsync(user, role));
        }
    }

    private static void EnsureSuccess(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
        }
    }
}
