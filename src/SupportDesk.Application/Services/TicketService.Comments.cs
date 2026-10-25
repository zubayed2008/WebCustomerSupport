using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Contracts;
using SupportDesk.Domain;
using SupportDesk.Domain.Common;
using SupportDesk.Domain.Permissions;
using SupportDesk.Domain.Sla;
using SupportDesk.Domain.Tickets;

namespace SupportDesk.Application.Services;

public sealed partial class TicketService
{
    public async Task<TicketDetailDto> AddCommentAsync(Guid id, AddCommentRequest request, CancellationToken cancellationToken)
    {
        var userId = RequireAuthenticated();
        var ticket = await LoadDetailAsync(id, cancellationToken);
        await EnsureCanReadAsync(ticket, cancellationToken);

        var isSupport = _currentUser.HasPermission(Permissions.TicketsViewAll);
        if (request.IsInternal)
        {
            RequirePermission(Permissions.TicketsNote);
            if (!isSupport)
            {
                throw new ForbiddenException("Customers cannot create internal notes.");
            }
        }
        else
        {
            RequirePermission(Permissions.TicketsReply);
        }

        var now = _clock.UtcNow;
        var isFirstResponse = !request.IsInternal && isSupport && ticket.FirstRespondedAt is null;
        var visibility = request.IsInternal ? CommentVisibility.Internal : CommentVisibility.Public;
        var comment = TicketComment.Create(ticket.Id, userId, request.Body, visibility, isFirstResponse, now);
        _dbContext.TicketComments.Add(comment);

        if (isFirstResponse)
        {
            ticket.RecordPublicResponse(now);
        }

        if (!request.IsInternal)
        {
            if (isSupport && ticket.Status == TicketStatus.New)
            {
                ticket.Transition(TicketStatus.Open, now);
            }
            else if (!isSupport && ticket.Status == TicketStatus.PendingCustomer)
            {
                var paused = await ComputePausedMinutesAsync(ticket, now, cancellationToken);
                ticket.Transition(TicketStatus.Open, now, paused);
            }
        }
        else
        {
            ticket.Touch(now);
        }

        _dbContext.TicketActivities.Add(TicketActivity.Record(
            ticket.Id,
            userId,
            request.IsInternal ? ActivityType.InternalNote : ActivityType.PublicReply,
            now,
            correlationId: _currentUser.CorrelationId));

        if (!request.IsInternal)
        {
            if (ticket.RequesterId != userId)
            {
                await _notifications.NotifyReplyAsync(ticket, ticket.RequesterId, "Support", false, cancellationToken);
            }

            if (ticket.AssigneeId is not null && ticket.AssigneeId != userId)
            {
                await _notifications.NotifyReplyAsync(ticket, ticket.AssigneeId.Value, "Requester", false, cancellationToken);
            }
        }
        else if (ticket.AssigneeId is not null && ticket.AssigneeId != userId)
        {
            await _notifications.NotifyReplyAsync(ticket, ticket.AssigneeId.Value, "Team member", true, cancellationToken);
        }

        await SaveChangesAsync(cancellationToken);
        return await GetAsync(ticket.Id, cancellationToken);
    }

    public async Task<TicketDetailDto> TransitionAsync(Guid id, TicketStatus target, CancellationToken cancellationToken)
    {
        var userId = RequireAuthenticated();
        var ticket = await LoadDetailAsync(id, cancellationToken);
        await EnsureCanReadAsync(ticket, cancellationToken);

        var isSupport = _currentUser.HasPermission(Permissions.TicketsViewAll);
        if (isSupport)
        {
            RequirePermission(Permissions.TicketsUpdateStatus);
        }
        else if (!(ticket.RequesterId == userId && target == TicketStatus.Open && ticket.Status is TicketStatus.Resolved or TicketStatus.Closed))
        {
            throw new ForbiddenException("You cannot change this ticket status.");
        }

        var now = _clock.UtcNow;
        var previous = ticket.Status;
        var paused = await ComputePausedMinutesAsync(ticket, now, cancellationToken);
        ticket.Transition(target, now, paused);

        var activityType = target switch
        {
            TicketStatus.Resolved => ActivityType.Resolved,
            TicketStatus.Closed => ActivityType.Closed,
            TicketStatus.Open when TicketWorkflow.IsReopen(previous, target) => ActivityType.Reopened,
            _ => ActivityType.StatusChanged,
        };

        _dbContext.TicketActivities.Add(TicketActivity.Record(
            ticket.Id,
            userId,
            activityType,
            now,
            field: nameof(Ticket.Status),
            oldValue: previous.ToString(),
            newValue: target.ToString(),
            correlationId: _currentUser.CorrelationId));

        if (ticket.RequesterId != userId)
        {
            await _notifications.NotifyStatusChangedAsync(ticket, ticket.RequesterId, previous, cancellationToken);
        }

        if (ticket.AssigneeId is not null && ticket.AssigneeId != userId)
        {
            await _notifications.NotifyStatusChangedAsync(ticket, ticket.AssigneeId.Value, previous, cancellationToken);
        }

        await SaveChangesAsync(cancellationToken);
        return await GetAsync(ticket.Id, cancellationToken);
    }

    private async Task<int> ComputePausedMinutesAsync(Ticket ticket, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (ticket.PendingCustomerSince is null || ticket.SlaPolicyId is null)
        {
            return 0;
        }

        var calendar = await _dbContext.BusinessCalendars
            .Include(x => x.Holidays)
            .FirstOrDefaultAsync(x => x.Id == ticket.SlaPolicy!.BusinessCalendarId, cancellationToken);

        return calendar is null ? 0 : SlaCalculator.BusinessMinutesBetween(ticket.PendingCustomerSince.Value, now, calendar);
    }
}
