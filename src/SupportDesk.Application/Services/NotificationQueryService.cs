using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts;
using SupportDesk.Domain.Common;

namespace SupportDesk.Application.Services;

public sealed class NotificationQueryService
{
    private readonly IAppDbContext _dbContext;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;

    public NotificationQueryService(IAppDbContext dbContext, IClock clock, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<NotificationDto>> ListAsync(bool unreadOnly, int page, int pageSize, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException("Authentication is required.");
        var query = _dbContext.Notifications.AsNoTracking().Where(x => x.RecipientId == userId);
        if (unreadOnly)
        {
            query = query.Where(x => !x.IsRead);
        }

        page = Math.Clamp(page, 1, 10000);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rows.Select(x => new NotificationDto(x.Id, x.Type, x.Title, x.Body, x.Link, x.IsRead, x.CreatedAt, x.ReadAt)).ToList();
        return new PagedResult<NotificationDto>(items, page, pageSize, total);
    }

    public async Task<int> UnreadCountAsync(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException("Authentication is required.");
        return await _dbContext.Notifications.CountAsync(x => x.RecipientId == userId && !x.IsRead, cancellationToken);
    }

    public async Task MarkReadAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException("Authentication is required.");
        var notification = await _dbContext.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.RecipientId == userId, cancellationToken)
            ?? throw new NotFoundException("Notification not found.");
        notification.MarkRead(_clock.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllReadAsync(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException("Authentication is required.");
        var now = _clock.UtcNow;
        var unread = await _dbContext.Notifications.Where(x => x.RecipientId == userId && !x.IsRead).ToListAsync(cancellationToken);
        foreach (var notification in unread)
        {
            notification.MarkRead(now);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
