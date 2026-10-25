using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportDesk.Application.Contracts;
using SupportDesk.Application.Services;

namespace SupportDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly NotificationQueryService _notifications;

    public NotificationsController(NotificationQueryService notifications)
    {
        _notifications = notifications;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<NotificationDto>>> List([FromQuery] bool unreadOnly = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        return Ok(await _notifications.ListAsync(unreadOnly, page, pageSize, cancellationToken));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> UnreadCount(CancellationToken cancellationToken)
    {
        return Ok(await _notifications.UnreadCountAsync(cancellationToken));
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        await _notifications.MarkReadAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        await _notifications.MarkAllReadAsync(cancellationToken);
        return NoContent();
    }
}
