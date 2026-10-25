using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts;
using SupportDesk.Domain.Common;
using SupportDesk.Domain.Permissions;
using SupportDesk.Infrastructure.Identity;
using SupportDesk.Infrastructure.Persistence;

namespace SupportDesk.Infrastructure.Services;

public sealed partial class AdminService : IAdminService
{
    private readonly SupportDeskDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditWriter _audit;

    public AdminService(
        SupportDeskDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IClock clock,
        ICurrentUser currentUser,
        IAuditWriter audit)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _clock = clock;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<PagedResult<UserSummaryDto>> ListUsersAsync(string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (!_currentUser.HasPermission(Permissions.UsersView) && !_currentUser.HasPermission(Permissions.UsersManage))
        {
            throw new ForbiddenException("Permission to view users is required.");
        }

        var query = _dbContext.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            if (term.Length > 100)
            {
                term = term[..100];
            }

            query = query.Where(x => x.Email!.ToLower().Contains(term) || x.DisplayName.ToLower().Contains(term));
        }

        page = Math.Clamp(page, 1, 10000);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var total = await query.CountAsync(cancellationToken);
        var users = await query.OrderBy(x => x.Email).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var items = new List<UserSummaryDto>();
        foreach (var user in users)
        {
            items.Add(await MapUserAsync(user));
        }

        return new PagedResult<UserSummaryDto>(items, page, pageSize, total);
    }

    public async Task<UserSummaryDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        RequireAdmin();
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            throw new ConflictException("A user with this email already exists.");
        }

        var now = _clock.UtcNow;
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = request.DisplayName.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        EnsureIdentitySuccess(result);

        if (request.Roles is { Count: > 0 })
        {
            await AssignRolesAsync(user, request.Roles, cancellationToken);
        }

        await _audit.WriteAsync(_currentUser.UserId, "user.create", nameof(ApplicationUser), user.Id.ToString(), new { user.Email }, cancellationToken);
        return await MapUserAsync(user);
    }

    private async Task<UserSummaryDto> MapUserAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return new UserSummaryDto(user.Id, user.Email ?? string.Empty, user.DisplayName, user.IsActive, roles.OrderBy(x => x).ToList(), user.CreatedAt);
    }

    private void RequireAdmin()
    {
        if (!_currentUser.HasPermission(Permissions.UsersManage) && !_currentUser.HasPermission(Permissions.RolesManage))
        {
            throw new ForbiddenException("Administrative permission is required.");
        }
    }

    private static void EnsureIdentitySuccess(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            var message = string.Join("; ", result.Errors.Select(x => x.Description));
            throw new ValidationFailedException(string.IsNullOrWhiteSpace(message) ? "The identity operation failed." : message);
        }
    }
}
