using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts;
using SupportDesk.Infrastructure.Identity;
using SupportDesk.Infrastructure.Persistence;

namespace SupportDesk.Infrastructure.Services;

public sealed class UserDirectory : IUserDirectory
{
    private readonly SupportDeskDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserDirectory(SupportDeskDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return await _dbContext.Users
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => string.IsNullOrWhiteSpace(x.DisplayName) ? x.UserName ?? "Unknown" : x.DisplayName, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetEmailsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return await _dbContext.Users
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id) && x.Email != null)
            .ToDictionaryAsync(x => x.Id, x => x.Email!, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetUserIdsInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        var role = await _dbContext.Roles.AsNoTracking().FirstOrDefaultAsync(x => x.Name == roleName, cancellationToken);
        if (role is null)
        {
            return [];
        }

        return await _dbContext.UserRoles
            .AsNoTracking()
            .Where(x => x.RoleId == role.Id)
            .Select(x => x.UserId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsActiveUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.Users.AsNoTracking().AnyAsync(x => x.Id == userId && x.IsActive, cancellationToken);
    }

    public async Task<IReadOnlyList<AssigneeDto>> GetSupportUsersAsync(CancellationToken cancellationToken)
    {
        var supportRoleNames = new[] { Domain.Permissions.SystemRoles.Agent, Domain.Permissions.SystemRoles.Manager, Domain.Permissions.SystemRoles.Administrator };
        var roleIds = _dbContext.Roles.Where(x => supportRoleNames.Contains(x.Name!)).Select(x => x.Id);
        var userIds = _dbContext.UserRoles.Where(x => roleIds.Contains(x.RoleId)).Select(x => x.UserId);

        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(x => x.IsActive && userIds.Contains(x.Id))
            .OrderBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);

        var result = new List<AssigneeDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new AssigneeDto(user.Id, user.DisplayName, user.Email ?? string.Empty, roles.OrderBy(x => x).ToList()));
        }

        return result;
    }
}
