using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts;
using SupportDesk.Domain;
using SupportDesk.Domain.Common;
using SupportDesk.Domain.Permissions;

namespace SupportDesk.Application.Services;

public sealed partial class ReportService
{
    private readonly IAppDbContext _dbContext;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly IUserDirectory _users;

    public ReportService(IAppDbContext dbContext, IClock clock, ICurrentUser currentUser, IUserDirectory users)
    {
        _dbContext = dbContext;
        _clock = clock;
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<ReportOverviewDto> GetOverviewAsync(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException("Authentication is required.");
        var canViewAll = _currentUser.HasPermission(Permissions.ReportsViewAll);
        if (!canViewAll && !_currentUser.HasPermission(Permissions.ReportsViewOwn))
        {
            throw new ForbiddenException("You do not have permission to view reports.");
        }

        var now = _clock.UtcNow;
        var periodStart = now.AddDays(-30);
        var tickets = _dbContext.Tickets.AsNoTracking().AsQueryable();
        if (!canViewAll)
        {
            tickets = tickets.Where(x => x.RequesterId == userId);
        }

        var activeRows = await tickets
            .Where(x => x.Status != TicketStatus.Resolved && x.Status != TicketStatus.Closed)
            .Select(x => new
            {
                x.Status,
                x.Priority,
                x.AssigneeId,
                x.FirstResponseDueAt,
                x.FirstRespondedAt,
                x.ResolutionDueAt,
                DueSoonMinutes = x.SlaPolicy != null ? x.SlaPolicy.DueSoonMinutes : 60,
            })
            .ToListAsync(cancellationToken);

        var resolvedRows = await tickets
            .Where(x => x.ResolvedAt != null && x.ResolvedAt >= periodStart)
            .Select(x => new { x.CreatedAt, x.FirstRespondedAt, x.ResolvedAt, x.PausedMinutes })
            .ToListAsync(cancellationToken);

        var trendRows = await tickets
            .Where(x => x.CreatedAt >= now.AddDays(-14) || (x.ResolvedAt != null && x.ResolvedAt >= now.AddDays(-14)))
            .Select(x => new { x.CreatedAt, x.ResolvedAt })
            .ToListAsync(cancellationToken);

        var active = activeRows
            .Select(x => new ActiveTicketRow(
                x.Status,
                x.Priority,
                x.AssigneeId,
                IsOverdue(x.FirstRespondedAt, x.FirstResponseDueAt, x.ResolutionDueAt, now),
                IsDueSoon(x.FirstRespondedAt, x.FirstResponseDueAt, x.ResolutionDueAt, x.DueSoonMinutes, now)))
            .ToList();

        return await BuildOverviewAsync(active, resolvedRows.Select(x => new ResolvedRow(x.CreatedAt, x.FirstRespondedAt, x.ResolvedAt, x.PausedMinutes)).ToList(), trendRows.Select(x => new TrendRow(x.CreatedAt, x.ResolvedAt)).ToList(), now, cancellationToken);
    }

    private sealed record ActiveTicketRow(TicketStatus Status, TicketPriority Priority, Guid? AssigneeId, bool Overdue, bool DueSoon);

    private sealed record ResolvedRow(DateTimeOffset CreatedAt, DateTimeOffset? FirstRespondedAt, DateTimeOffset? ResolvedAt, int PausedMinutes);

    private sealed record TrendRow(DateTimeOffset CreatedAt, DateTimeOffset? ResolvedAt);
}
