namespace SupportDesk.Application.Abstractions;

public interface IUserDirectory
{
    Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, string>> GetEmailsAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> GetUserIdsInRoleAsync(string roleName, CancellationToken cancellationToken);

    Task<bool> IsActiveUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Contracts.AssigneeDto>> GetSupportUsersAsync(CancellationToken cancellationToken);
}
