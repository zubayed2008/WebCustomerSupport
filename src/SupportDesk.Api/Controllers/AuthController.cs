using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts;
using SupportDesk.Infrastructure.Identity;

namespace SupportDesk.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IPermissionService _permissions;
    private readonly ICurrentUser _currentUser;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IPermissionService permissions,
        ICurrentUser currentUser)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _permissions = permissions;
        _currentUser = currentUser;
    }

    [HttpGet("antiforgery")]
    [AllowAnonymous]
    public IActionResult GetAntiforgeryToken([FromServices] Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<CurrentUserDto>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user is null || !user.IsActive)
        {
            return UnauthorizedProblem("Email or password is incorrect.");
        }

        var result = await _signInManager.PasswordSignInAsync(user, request.Password, isPersistent: false, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            return UnauthorizedProblem("The account is temporarily locked. Try again later.");
        }

        if (!result.Succeeded)
        {
            return UnauthorizedProblem("Email or password is incorrect.");
        }

        return Ok(await BuildCurrentUserAsync(user, cancellationToken));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new MessageResponse("Signed out."));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserDto>> Me(CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(_currentUser.UserId.Value.ToString());
        if (user is null || !user.IsActive)
        {
            return Unauthorized();
        }

        return Ok(await BuildCurrentUserAsync(user, cancellationToken));
    }

    [HttpPost("change-password")]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(_currentUser.UserId.Value.ToString());
        if (user is null)
        {
            return Unauthorized();
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["password"] = result.Errors.Select(x => x.Description).ToArray(),
            }));
        }

        await _userManager.UpdateSecurityStampAsync(user);
        await _signInManager.RefreshSignInAsync(user);
        return Ok(new MessageResponse("Password changed."));
    }

    private async Task<CurrentUserDto> BuildCurrentUserAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await _permissions.GetPermissionsForUserAsync(user.Id, cancellationToken);
        return new CurrentUserDto(user.Id, user.Email ?? string.Empty, user.DisplayName, roles.OrderBy(x => x).ToList(), permissions.OrderBy(x => x).ToList());
    }

    private UnauthorizedObjectResult UnauthorizedProblem(string detail)
    {
        return Unauthorized(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Authentication failed.",
            Detail = detail,
            Type = "https://httpstatuses.com/401",
        });
    }
}
