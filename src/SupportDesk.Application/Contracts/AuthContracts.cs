using System.ComponentModel.DataAnnotations;

namespace SupportDesk.Application.Contracts;

public sealed record LoginRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MaxLength(200)] string Password);

public sealed record RegisterRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MinLength(12), MaxLength(200)] string Password,
    [Required, MaxLength(150)] string DisplayName);

public sealed record CurrentUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public sealed record UserSummaryDto(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsActive,
    IReadOnlyList<string> Roles,
    DateTimeOffset CreatedAt);

public sealed record RoleDto(
    Guid Id,
    string Name,
    bool IsSystem,
    IReadOnlyList<string> Permissions);

public sealed record PermissionDto(
    Guid Id,
    string Name,
    string Group,
    string Description,
    bool IsSystem);

public sealed record CreateUserRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MinLength(12), MaxLength(200)] string Password,
    [Required, MaxLength(150)] string DisplayName,
    IReadOnlyList<string>? Roles);

public sealed record UpdateUserRequest(
    [Required, MaxLength(150)] string DisplayName,
    bool IsActive,
    IReadOnlyList<string>? Roles);

public sealed record SetPasswordRequest(
    [Required, MinLength(12), MaxLength(200)] string NewPassword);

public sealed record ChangePasswordRequest(
    [Required, MaxLength(200)] string CurrentPassword,
    [Required, MinLength(12), MaxLength(200)] string NewPassword);

public sealed record CreateRoleRequest([Required, MaxLength(100)] string Name);

public sealed record RenameRoleRequest([Required, MaxLength(100)] string Name);

public sealed record GrantPermissionsRequest(IReadOnlyList<string> PermissionNames);

public sealed record CreatePermissionRequest(
    [Required, MaxLength(100)] string Name,
    [Required, MaxLength(100)] string Group,
    [Required, MaxLength(500)] string Description);

