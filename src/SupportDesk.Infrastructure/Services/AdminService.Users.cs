using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Contracts;
using SupportDesk.Domain.Common;
using SupportDesk.Domain.Permissions;
using SupportDesk.Infrastructure.Identity;

namespace SupportDesk.Infrastructure.Services;

public sealed partial class AdminService
{
    public async Task<UserSummaryDto> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        RequireAdmin();
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User not found.");

        if (user.Id == _currentUser.UserId && !request.IsActive)
        {
            throw new ValidationFailedException("You cannot deactivate your own account.");
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var requestedRoles = (request.Roles ?? currentRoles.ToList()).Distinct().ToList();

        if (currentRoles.Contains(SystemRoles.Administrator) && !requestedRoles.Contains(SystemRoles.Administrator))
        {
            await EnsureAnotherAdministratorExistsAsync(user.Id, cancellationToken);
        }

        user.DisplayName = request.DisplayName.Trim();
        user.IsActive = request.IsActive;
        user.UpdatedAt = _clock.UtcNow;
        EnsureIdentitySuccess(await _userManager.UpdateAsync(user));

        if (!currentRoles.OrderBy(x => x).SequenceEqual(requestedRoles.OrderBy(x => x)))
        {
            EnsureIdentitySuccess(await _userManager.RemoveFromRolesAsync(user, currentRoles));
            await AssignRolesAsync(user, requestedRoles, cancellationToken);
        }

        if (!request.IsActive)
        {
            await _userManager.UpdateSecurityStampAsync(user);
        }
        else
        {
            // Reactivating an account clears any lockout so the user can sign in.
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);
        }

        await _audit.WriteAsync(_currentUser.UserId, "user.update", nameof(ApplicationUser), user.Id.ToString(), new { user.Email, user.IsActive, Roles = requestedRoles }, cancellationToken);
        return await MapUserAsync(user);
    }

    public async Task SetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken)
    {
        RequireAdmin();
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User not found.");
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        EnsureIdentitySuccess(await _userManager.ResetPasswordAsync(user, token, newPassword));

        // A password reset is the administrator recovery path, so it also clears
        // any lockout from failed sign-in attempts.
        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);
        await _userManager.UpdateSecurityStampAsync(user);
        await _audit.WriteAsync(_currentUser.UserId, "user.password-reset", nameof(ApplicationUser), user.Id.ToString(), null, cancellationToken);
    }

    private async Task AssignRolesAsync(ApplicationUser user, IReadOnlyList<string> roles, CancellationToken cancellationToken)
    {
        foreach (var roleName in roles.Distinct())
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                throw new ValidationFailedException($"Role '{roleName}' does not exist.");
            }

            EnsureIdentitySuccess(await _userManager.AddToRoleAsync(user, roleName));
        }

        await _userManager.UpdateSecurityStampAsync(user);
    }

    private async Task EnsureAnotherAdministratorExistsAsync(Guid excludedUserId, CancellationToken cancellationToken)
    {
        var adminRole = await _roleManager.FindByNameAsync(SystemRoles.Administrator)
            ?? throw new ConflictException("The Administrator role is missing.");

        var otherAdmins = await _dbContext.UserRoles
            .Where(x => x.RoleId == adminRole.Id && x.UserId != excludedUserId)
            .Join(_dbContext.Users.Where(x => x.IsActive), x => x.UserId, x => x.Id, (role, user) => user.Id)
            .CountAsync(cancellationToken);

        if (otherAdmins == 0)
        {
            throw new ValidationFailedException("At least one active administrator must remain.");
        }
    }
}
