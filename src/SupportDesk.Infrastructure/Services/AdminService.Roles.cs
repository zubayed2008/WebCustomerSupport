using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Contracts;
using SupportDesk.Domain.Common;
using SupportDesk.Domain.Permissions;
using SupportDesk.Infrastructure.Identity;

namespace SupportDesk.Infrastructure.Services;

public sealed partial class AdminService
{
    public async Task<IReadOnlyList<RoleDto>> ListRolesAsync(CancellationToken cancellationToken)
    {
        if (!_currentUser.HasPermission(Permissions.RolesView) && !_currentUser.HasPermission(Permissions.RolesManage))
        {
            throw new ForbiddenException("Permission to view roles is required.");
        }

        var roles = await _dbContext.Roles.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        var items = new List<RoleDto>();
        foreach (var role in roles)
        {
            items.Add(await MapRoleAsync(role, cancellationToken));
        }

        return items;
    }

    public async Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        RequireAdmin();
        var name = request.Name.Trim();
        if (await _roleManager.RoleExistsAsync(name))
        {
            throw new ConflictException("A role with this name already exists.");
        }

        var role = new ApplicationRole { Name = name, IsSystem = false, CreatedAt = _clock.UtcNow };
        EnsureIdentitySuccess(await _roleManager.CreateAsync(role));
        await _audit.WriteAsync(_currentUser.UserId, "role.create", nameof(ApplicationRole), role.Id.ToString(), new { role.Name }, cancellationToken);
        return await MapRoleAsync(role, cancellationToken);
    }

    public async Task<RoleDto> RenameRoleAsync(Guid roleId, string name, CancellationToken cancellationToken)
    {
        RequireAdmin();
        var role = await _roleManager.FindByIdAsync(roleId.ToString())
            ?? throw new NotFoundException("Role not found.");

        if (role.IsSystem)
        {
            throw new ValidationFailedException("System roles cannot be renamed.");
        }

        var trimmed = name.Trim();
        if (await _roleManager.RoleExistsAsync(trimmed))
        {
            throw new ConflictException("A role with this name already exists.");
        }

        role.Name = trimmed;
        EnsureIdentitySuccess(await _roleManager.UpdateAsync(role));
        await _audit.WriteAsync(_currentUser.UserId, "role.rename", nameof(ApplicationRole), role.Id.ToString(), new { role.Name }, cancellationToken);
        return await MapRoleAsync(role, cancellationToken);
    }

    public async Task DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken)
    {
        RequireAdmin();
        var role = await _roleManager.FindByIdAsync(roleId.ToString())
            ?? throw new NotFoundException("Role not found.");

        if (role.IsSystem)
        {
            throw new ValidationFailedException("System roles cannot be deleted.");
        }

        if (await _dbContext.UserRoles.AnyAsync(x => x.RoleId == role.Id, cancellationToken))
        {
            throw new ConflictException("Remove the role from all users before deleting it.");
        }

        EnsureIdentitySuccess(await _roleManager.DeleteAsync(role));
        await _audit.WriteAsync(_currentUser.UserId, "role.delete", nameof(ApplicationRole), role.Id.ToString(), new { role.Name }, cancellationToken);
    }
}
