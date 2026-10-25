using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts;
using SupportDesk.Domain.Common;
using SupportDesk.Domain.Permissions;
using SupportDesk.Infrastructure.Identity;

namespace SupportDesk.Infrastructure.Services;

public sealed partial class AdminService
{
    public async Task<RoleDto> GrantPermissionsAsync(Guid roleId, IReadOnlyList<string> permissionNames, CancellationToken cancellationToken)
    {
        RequireAdmin();
        var role = await _roleManager.FindByIdAsync(roleId.ToString())
            ?? throw new NotFoundException("Role not found.");

        var requested = permissionNames.Select(x => x.Trim().ToLowerInvariant()).Where(x => x.Length > 0).Distinct().ToList();
        var permissions = await _dbContext.Permissions.Where(x => requested.Contains(x.Name)).ToListAsync(cancellationToken);
        var missing = requested.Except(permissions.Select(x => x.Name)).ToList();
        if (missing.Count > 0)
        {
            throw new ValidationFailedException($"Unknown permissions: {string.Join(", ", missing)}.");
        }

        if (role.Name == SystemRoles.Administrator
            && (!requested.Contains(Permissions.RolesManage) || !requested.Contains(Permissions.PermissionsManage)))
        {
            throw new ValidationFailedException("The Administrator role must keep role and permission management.");
        }

        var existing = await _dbContext.RolePermissions.Where(x => x.RoleId == role.Id).ToListAsync(cancellationToken);
        _dbContext.RolePermissions.RemoveRange(existing);
        foreach (var permission in permissions)
        {
            _dbContext.RolePermissions.Add(new ApplicationRolePermission { RoleId = role.Id, PermissionId = permission.Id });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var userIds = await _dbContext.UserRoles.Where(x => x.RoleId == role.Id).Select(x => x.UserId).ToListAsync(cancellationToken);
        foreach (var userId in userIds)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is not null)
            {
                await _userManager.UpdateSecurityStampAsync(user);
            }
        }

        await _audit.WriteAsync(_currentUser.UserId, "role.permissions", nameof(ApplicationRole), role.Id.ToString(), new { role.Name, Permissions = requested }, cancellationToken);
        return await MapRoleAsync(role, cancellationToken);
    }

    public async Task<IReadOnlyList<PermissionDto>> ListPermissionsAsync(CancellationToken cancellationToken)
    {
        if (!_currentUser.HasPermission(Permissions.RolesView) && !_currentUser.HasPermission(Permissions.PermissionsManage))
        {
            throw new ForbiddenException("Permission to view permissions is required.");
        }

        return await _dbContext.Permissions
            .AsNoTracking()
            .OrderBy(x => x.Group).ThenBy(x => x.Name)
            .Select(x => new PermissionDto(x.Id, x.Name, x.Group, x.Description, x.IsSystem))
            .ToListAsync(cancellationToken);
    }

    public async Task<PermissionDto> CreatePermissionAsync(CreatePermissionRequest request, CancellationToken cancellationToken)
    {
        RequireAdmin();
        var permission = Permission.Create(request.Name, request.Group, request.Description, false, _clock.UtcNow);
        if (await _dbContext.Permissions.AnyAsync(x => x.Name == permission.Name, cancellationToken))
        {
            throw new ConflictException("A permission with this name already exists.");
        }

        _dbContext.Permissions.Add(permission);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(_currentUser.UserId, "permission.create", nameof(Permission), permission.Id.ToString(), new { permission.Name }, cancellationToken);
        return new PermissionDto(permission.Id, permission.Name, permission.Group, permission.Description, permission.IsSystem);
    }

    public async Task<IReadOnlyList<AuditLogDto>> ListAuditAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        if (!_currentUser.HasPermission(Permissions.AuditView))
        {
            throw new ForbiddenException("Audit permission is required.");
        }

        page = Math.Clamp(page, 1, 10000);
        pageSize = Math.Clamp(pageSize, 1, 100);
        return await _dbContext.AuditLogs
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AuditLogDto(x.Id, x.ActorId, x.Action, x.EntityType, x.EntityId, x.DetailsJson, x.IpAddress, x.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    private async Task<RoleDto> MapRoleAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        var permissions = await _dbContext.RolePermissions
            .AsNoTracking()
            .Where(x => x.RoleId == role.Id)
            .Select(x => x.Permission!.Name)
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        return new RoleDto(role.Id, role.Name ?? string.Empty, role.IsSystem, permissions);
    }
}
