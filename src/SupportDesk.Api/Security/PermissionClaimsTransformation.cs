using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using SupportDesk.Application.Abstractions;

namespace SupportDesk.Api.Security;

/// <summary>
/// Materializes database permissions as claims. A short security-stamp
/// validation interval means revoked permissions are refreshed promptly.
/// </summary>
public sealed class PermissionClaimsTransformation : IClaimsTransformation
{
    private const string CacheKey = "supportdesk.permissions.loaded";

    private readonly IPermissionService _permissions;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PermissionClaimsTransformation(IPermissionService permissions, IHttpContextAccessor httpContextAccessor)
    {
        _permissions = permissions;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return principal;
        }

        if (principal.HasClaim(claim => claim.Type == HttpCurrentUser.PermissionClaimType))
        {
            return principal;
        }

        var context = _httpContextAccessor.HttpContext;
        if (context is not null && context.Items.ContainsKey(CacheKey))
        {
            return principal;
        }

        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(value, out var userId))
        {
            return principal;
        }

        var permissions = await _permissions.GetPermissionsForUserAsync(userId, CancellationToken.None);
        var identity = new ClaimsIdentity();
        foreach (var permission in permissions)
        {
            identity.AddClaim(new Claim(HttpCurrentUser.PermissionClaimType, permission));
        }

        if (context is not null)
        {
            context.Items[CacheKey] = true;
        }

        principal.AddIdentity(identity);
        return principal;
    }
}
