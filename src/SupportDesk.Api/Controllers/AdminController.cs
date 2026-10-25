using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportDesk.Api.Security;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts;
using SupportDesk.Domain.Permissions;

namespace SupportDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/admin/users")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IAdminService _admin;

    public AdminUsersController(IAdminService admin)
    {
        _admin = admin;
    }

    [HttpGet]
    [HasPermission(Permissions.UsersView)]
    public async Task<ActionResult<PagedResult<UserSummaryDto>>> List([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        return Ok(await _admin.ListUsersAsync(search, page, pageSize, cancellationToken));
    }

    [HttpPost]
    [HasPermission(Permissions.UsersManage)]
    public async Task<ActionResult<UserSummaryDto>> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _admin.CreateUserAsync(request, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.UsersManage)]
    public async Task<ActionResult<UserSummaryDto>> Update(Guid id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _admin.UpdateUserAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/password")]
    [HasPermission(Permissions.UsersManage)]
    public async Task<IActionResult> SetPassword(Guid id, SetPasswordRequest request, CancellationToken cancellationToken)
    {
        await _admin.SetPasswordAsync(id, request.NewPassword, cancellationToken);
        return NoContent();
    }
}

[ApiController]
[Authorize]
[Route("api/v1/admin/roles")]
public sealed class AdminRolesController : ControllerBase
{
    private readonly IAdminService _admin;

    public AdminRolesController(IAdminService admin)
    {
        _admin = admin;
    }

    [HttpGet]
    [HasPermission(Permissions.RolesView)]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> List(CancellationToken cancellationToken)
    {
        return Ok(await _admin.ListRolesAsync(cancellationToken));
    }

    [HttpPost]
    [HasPermission(Permissions.RolesManage)]
    public async Task<ActionResult<RoleDto>> Create(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _admin.CreateRoleAsync(request, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.RolesManage)]
    public async Task<ActionResult<RoleDto>> Rename(Guid id, RenameRoleRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _admin.RenameRoleAsync(id, request.Name, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.RolesManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _admin.DeleteRoleAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/permissions")]
    [HasPermission(Permissions.PermissionsManage)]
    public async Task<ActionResult<RoleDto>> Grant(Guid id, GrantPermissionsRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _admin.GrantPermissionsAsync(id, request.PermissionNames, cancellationToken));
    }
}

[ApiController]
[Authorize]
[Route("api/v1/admin/permissions")]
public sealed class AdminPermissionsController : ControllerBase
{
    private readonly IAdminService _admin;

    public AdminPermissionsController(IAdminService admin)
    {
        _admin = admin;
    }

    [HttpGet]
    [HasPermission(Permissions.RolesView)]
    public async Task<ActionResult<IReadOnlyList<PermissionDto>>> List(CancellationToken cancellationToken)
    {
        return Ok(await _admin.ListPermissionsAsync(cancellationToken));
    }

    [HttpPost]
    [HasPermission(Permissions.PermissionsManage)]
    public async Task<ActionResult<PermissionDto>> Create(CreatePermissionRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _admin.CreatePermissionAsync(request, cancellationToken));
    }
}

[ApiController]
[Authorize]
[Route("api/v1/admin/audit")]
public sealed class AdminAuditController : ControllerBase
{
    private readonly IAdminService _admin;

    public AdminAuditController(IAdminService admin)
    {
        _admin = admin;
    }

    [HttpGet]
    [HasPermission(Permissions.AuditView)]
    public async Task<ActionResult<IReadOnlyList<AuditLogDto>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        return Ok(await _admin.ListAuditAsync(page, pageSize, cancellationToken));
    }
}
