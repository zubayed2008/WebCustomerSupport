namespace SupportDesk.Application.Abstractions;

public interface IPermissionService
{
    Task<IReadOnlyCollection<string>> GetPermissionsForUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<bool> UserHasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken);
}
