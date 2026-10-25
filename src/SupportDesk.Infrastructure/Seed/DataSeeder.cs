using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Abstractions;
using SupportDesk.Domain.Permissions;
using SupportDesk.Infrastructure.Identity;
using SupportDesk.Infrastructure.Persistence;

namespace SupportDesk.Infrastructure.Seed;

public sealed class SeedOptions
{
    public string AdminEmail { get; set; } = string.Empty;

    public string AdminPassword { get; set; } = string.Empty;

    public string? DemoPassword { get; set; }
}

public sealed partial class DataSeeder
{
    private readonly SupportDeskDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IClock _clock;
    private readonly SeedOptions _options;

    public DataSeeder(
        SupportDeskDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IClock clock,
        Microsoft.Extensions.Options.IOptions<SeedOptions> options)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _clock = clock;
        _options = options.Value;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        await SeedPermissionsAsync(now, cancellationToken);
        await SeedRolesAsync(cancellationToken);
        await SeedAdministratorAsync(cancellationToken);
        await SeedReferenceDataAsync(now, cancellationToken);
        await SeedDemoUsersAsync(cancellationToken);
        await SeedDemoContentAsync(now, cancellationToken);
    }

    private async Task SeedPermissionsAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Permissions.ToListAsync(cancellationToken);
        foreach (var definition in Permissions.All)
        {
            var permission = existing.FirstOrDefault(x => x.Name == definition.Name);
            if (permission is null)
            {
                _dbContext.Permissions.Add(Permission.Create(definition.Name, definition.Group, definition.Description, true, now));
            }
            else if (permission.Description != definition.Description || permission.Group != definition.Group)
            {
                permission.Group = definition.Group;
                permission.Description = definition.Description;
                permission.IsSystem = true;
                permission.UpdatedAt = now;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        foreach (var (name, permissionNames) in RoleDefinitions())
        {
            var role = await _roleManager.FindByNameAsync(name);
            if (role is null)
            {
                role = new ApplicationRole { Name = name, IsSystem = true, CreatedAt = _clock.UtcNow };
                EnsureSuccess(await _roleManager.CreateAsync(role));
            }

            var hasPermissions = await _dbContext.RolePermissions.AnyAsync(x => x.RoleId == role.Id, cancellationToken);
            if (!hasPermissions || name == SystemRoles.Administrator)
            {
                var permissions = await _dbContext.Permissions.Where(x => permissionNames.Contains(x.Name)).ToListAsync(cancellationToken);
                var existing = await _dbContext.RolePermissions.Where(x => x.RoleId == role.Id).ToListAsync(cancellationToken);
                _dbContext.RolePermissions.RemoveRange(existing);
                foreach (var permission in permissions)
                {
                    _dbContext.RolePermissions.Add(new ApplicationRolePermission { RoleId = role.Id, PermissionId = permission.Id });
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private static Dictionary<string, string[]> RoleDefinitions()
    {
        return new Dictionary<string, string[]>
        {
            [SystemRoles.Administrator] = Permissions.All.Select(x => x.Name).ToArray(),
            [SystemRoles.Manager] =
            [
                Permissions.TicketsViewAll, Permissions.TicketsCreate, Permissions.TicketsReply, Permissions.TicketsNote,
                Permissions.TicketsAssign, Permissions.TicketsUpdateStatus, Permissions.TicketsUpdatePriority, Permissions.TicketsUpdateCategory,
                Permissions.TicketsAttach, Permissions.KnowledgeViewPublished, Permissions.KnowledgeViewAll, Permissions.KnowledgeCreate,
                Permissions.KnowledgePublish, Permissions.KnowledgeArchive, Permissions.SlaView, Permissions.SlaManage,
                Permissions.ReportsViewAll, Permissions.UsersView, Permissions.RolesView, Permissions.AiUse, Permissions.AiReview,
            ],
            [SystemRoles.Agent] =
            [
                Permissions.TicketsViewAll, Permissions.TicketsCreate, Permissions.TicketsReply, Permissions.TicketsNote,
                Permissions.TicketsAssign, Permissions.TicketsUpdateStatus, Permissions.TicketsUpdatePriority, Permissions.TicketsUpdateCategory,
                Permissions.TicketsAttach, Permissions.KnowledgeViewPublished, Permissions.KnowledgeViewAll, Permissions.KnowledgeCreate,
                Permissions.SlaView, Permissions.ReportsViewAll, Permissions.AiUse, Permissions.AiReview,
            ],
            [SystemRoles.Customer] =
            [
                Permissions.TicketsViewOwn, Permissions.TicketsCreate, Permissions.TicketsReply, Permissions.TicketsAttach,
                Permissions.KnowledgeViewPublished, Permissions.ReportsViewOwn,
            ],
        };
    }
}
