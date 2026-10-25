using System.Security.Claims;
using SupportDesk.Application.Abstractions;
using SupportDesk.Domain.Permissions;

namespace SupportDesk.Api.Security;

public sealed class HttpCurrentUser : ICurrentUser
{
    public const string PermissionClaimType = "permission";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email);

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public IReadOnlyCollection<string> Permissions =>
        Principal?.FindAll(PermissionClaimType).Select(x => x.Value).Distinct().ToList() ?? [];

    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(x => x.Value).Distinct().ToList() ?? [];

    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? CorrelationId => _httpContextAccessor.HttpContext?.TraceIdentifier;

    public bool HasPermission(string permission)
    {
        return Principal?.HasClaim(PermissionClaimType, permission) == true;
    }
}
