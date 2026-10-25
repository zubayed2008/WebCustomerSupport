using Microsoft.AspNetCore.Identity;

namespace SupportDesk.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class ApplicationRole : IdentityRole<Guid>
{
    public bool IsSystem { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<ApplicationRolePermission> RolePermissions { get; set; } = [];
}

public sealed class ApplicationRolePermission
{
    public Guid RoleId { get; set; }

    public ApplicationRole? Role { get; set; }

    public Guid PermissionId { get; set; }

    public Domain.Permissions.Permission? Permission { get; set; }
}
