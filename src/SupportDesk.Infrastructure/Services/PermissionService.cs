using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Abstractions;
using SupportDesk.Infrastructure.Persistence;

namespace SupportDesk.Infrastructure.Services;

/// <summary>
/// Resolves effective permissions from the database on demand so administrator
/// changes take effect without redeploying or hardcoding role names.
/// </summary>
public sealed class PermissionService : IPermissionService
{
    private readonly SupportDeskDbContext _dbContext;

    public PermissionService(SupportDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<string>> GetPermissionsForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var roleIds = _dbContext.UserRoles
            .Where(x => x.UserId == userId)
            .Select(x => x.RoleId);

        return await _dbContext.RolePermissions
            .AsNoTracking()
            .Where(x => roleIds.Contains(x.RoleId))
            .Select(x => x.Permission!.Name)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> UserHasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken)
    {
        var roleIds = _dbContext.UserRoles
            .Where(x => x.UserId == userId)
            .Select(x => x.RoleId);

        return await _dbContext.RolePermissions
            .AsNoTracking()
            .AnyAsync(x => roleIds.Contains(x.RoleId) && x.Permission!.Name == permission, cancellationToken);
    }
}
