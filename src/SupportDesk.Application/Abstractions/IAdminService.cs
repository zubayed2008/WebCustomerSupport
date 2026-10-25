using SupportDesk.Application.Contracts;

namespace SupportDesk.Application.Abstractions;

public interface IAdminService
{
    Task<PagedResult<UserSummaryDto>> ListUsersAsync(string? search, int page, int pageSize, CancellationToken cancellationToken);

    Task<UserSummaryDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken);

    Task<UserSummaryDto> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken);

    Task SetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken);

    Task<IReadOnlyList<RoleDto>> ListRolesAsync(CancellationToken cancellationToken);

    Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken);

    Task<RoleDto> RenameRoleAsync(Guid roleId, string name, CancellationToken cancellationToken);

    Task DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken);

    Task<RoleDto> GrantPermissionsAsync(Guid roleId, IReadOnlyList<string> permissionNames, CancellationToken cancellationToken);

    Task<IReadOnlyList<PermissionDto>> ListPermissionsAsync(CancellationToken cancellationToken);

    Task<PermissionDto> CreatePermissionAsync(CreatePermissionRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<AuditLogDto>> ListAuditAsync(int page, int pageSize, CancellationToken cancellationToken);
}

public sealed record AuditLogDto(
    Guid Id,
    Guid? ActorId,
    string Action,
    string EntityType,
    string EntityId,
    string? DetailsJson,
    string? IpAddress,
    DateTimeOffset CreatedAt);
