using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Abstractions;
using SupportDesk.Domain;
using SupportDesk.Domain.Permissions;
using SupportDesk.Domain.Tickets;

namespace SupportDesk.Application.Services;

/// <summary>
/// Periodically marks SLA breaches and emits due-soon or overdue notifications.
/// Idempotency keys ensure retries do not duplicate notifications.
/// </summary>
public sealed class SlaMonitor
{
    private readonly IAppDbContext _dbContext;
    private readonly IClock _clock;
    private readonly INotificationService _notifications;
    private readonly IUserDirectory _users;

    public SlaMonitor(IAppDbContext dbContext, IClock clock, INotificationService notifications, IUserDirectory users)
    {
        _dbContext = dbContext;
        _clock = clock;
        _notifications = notifications;
        _users = users;
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var tickets = await _dbContext.Tickets
            .Include(x => x.SlaPolicy)
            .Where(x => x.Status != TicketStatus.Resolved && x.Status != TicketStatus.Closed)
            .Where(x => x.FirstResponseDueAt != null || x.ResolutionDueAt != null)
            .Take(500)
            .ToListAsync(cancellationToken);

        var changed = 0;
        foreach (var ticket in tickets)
        {
            var dueSoonMinutes = ticket.SlaPolicy?.DueSoonMinutes ?? 60;
            var warning = now.AddMinutes(Math.Max(1, dueSoonMinutes));

            if (ticket.FirstRespondedAt is null && ticket.FirstResponseDueAt is not null)
            {
                if (ticket.FirstResponseDueAt < now && !ticket.ResponseBreached)
                {
                    ticket.MarkResponseBreached(now);
                    await NotifyAsync(ticket, true, $"sla:response:{ticket.Id}:{ticket.FirstResponseDueAt.Value.UtcTicks}", cancellationToken);
                    changed++;
                }
                else if (ticket.FirstResponseDueAt >= now && ticket.FirstResponseDueAt <= warning)
                {
                    await NotifyAsync(ticket, false, $"sla:response-soon:{ticket.Id}:{ticket.FirstResponseDueAt.Value.UtcTicks}", cancellationToken);
                }
            }

            if (ticket.ResolutionDueAt is not null)
            {
                if (ticket.ResolutionDueAt < now && !ticket.ResolutionBreached)
                {
                    ticket.MarkResolutionBreached(now);
                    await NotifyAsync(ticket, true, $"sla:resolution:{ticket.Id}:{ticket.ResolutionDueAt.Value.UtcTicks}", cancellationToken);
                    changed++;
                }
                else if (ticket.ResolutionDueAt >= now && ticket.ResolutionDueAt <= warning)
                {
                    await NotifyAsync(ticket, false, $"sla:resolution-soon:{ticket.Id}:{ticket.ResolutionDueAt.Value.UtcTicks}", cancellationToken);
                }
            }
        }

        if (changed > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return changed;
    }

    private async Task NotifyAsync(Ticket ticket, bool overdue, string idempotencyKey, CancellationToken cancellationToken)
    {
        if (await _dbContext.OutboxMessages.AnyAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken))
        {
            return;
        }

        var recipients = new List<Guid>();
        if (ticket.AssigneeId is not null)
        {
            recipients.Add(ticket.AssigneeId.Value);
        }
        else
        {
            recipients.AddRange(await _users.GetUserIdsInRoleAsync(SystemRoles.Manager, cancellationToken));
        }

        foreach (var recipient in recipients.Distinct())
        {
            await _notifications.NotifySlaAsync(ticket, recipient, overdue, cancellationToken);
        }
    }
}
