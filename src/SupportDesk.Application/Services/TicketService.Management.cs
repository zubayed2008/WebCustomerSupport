using SupportDesk.Application.Contracts;
using SupportDesk.Domain;
using SupportDesk.Domain.Common;
using SupportDesk.Domain.Permissions;
using SupportDesk.Domain.Tickets;

namespace SupportDesk.Application.Services;

public sealed partial class TicketService
{
    public async Task<TicketDetailDto> AssignAsync(Guid id, Guid? assigneeId, CancellationToken cancellationToken)
    {
        var userId = RequireAuthenticated();
        RequirePermission(Permissions.TicketsAssign);
        var ticket = await LoadDetailAsync(id, cancellationToken);
        await EnsureCanReadAsync(ticket, cancellationToken);

        if (assigneeId is not null && !await _users.IsActiveUserAsync(assigneeId.Value, cancellationToken))
        {
            throw new ValidationFailedException("The selected assignee is not an active user.");
        }

        var now = _clock.UtcNow;
        var previous = ticket.AssigneeId;
        ticket.Assign(assigneeId, now);

        _dbContext.TicketActivities.Add(TicketActivity.Record(
            ticket.Id,
            userId,
            ActivityType.Assigned,
            now,
            field: nameof(Ticket.AssigneeId),
            oldValue: previous?.ToString(),
            newValue: assigneeId?.ToString(),
            correlationId: _currentUser.CorrelationId));

        if (assigneeId is not null && assigneeId != userId)
        {
            await _notifications.NotifyAssignedAsync(ticket, assigneeId.Value, cancellationToken);
        }

        await SaveChangesAsync(cancellationToken);
        return await GetAsync(ticket.Id, cancellationToken);
    }

    public async Task<TicketDetailDto> UpdateAsync(Guid id, UpdateTicketRequest request, CancellationToken cancellationToken)
    {
        var userId = RequireAuthenticated();
        var ticket = await LoadDetailAsync(id, cancellationToken);
        await EnsureCanReadAsync(ticket, cancellationToken);
        var now = _clock.UtcNow;
        var changed = false;

        if (request.Priority is not null && request.Priority != ticket.Priority)
        {
            RequirePermission(Permissions.TicketsUpdatePriority);
            var previous = ticket.Priority;
            ticket.ChangePriority(request.Priority.Value, now);
            _dbContext.TicketActivities.Add(TicketActivity.Record(ticket.Id, userId, ActivityType.PriorityChanged, now, nameof(Ticket.Priority), previous.ToString(), request.Priority.ToString(), _currentUser.CorrelationId));
            changed = true;
        }

        if (request.CategoryId is not null && request.CategoryId != ticket.CategoryId)
        {
            RequirePermission(Permissions.TicketsUpdateCategory);
            var previous = ticket.CategoryId;
            ticket.ChangeCategory(request.CategoryId, now);
            _dbContext.TicketActivities.Add(TicketActivity.Record(ticket.Id, userId, ActivityType.CategoryChanged, now, nameof(Ticket.CategoryId), previous?.ToString(), request.CategoryId.ToString(), _currentUser.CorrelationId));
            changed = true;
        }

        if (request.AssigneeId is not null && request.AssigneeId != ticket.AssigneeId)
        {
            RequirePermission(Permissions.TicketsAssign);
            if (!await _users.IsActiveUserAsync(request.AssigneeId.Value, cancellationToken))
            {
                throw new ValidationFailedException("The selected assignee is not an active user.");
            }

            var previous = ticket.AssigneeId;
            ticket.Assign(request.AssigneeId, now);
            _dbContext.TicketActivities.Add(TicketActivity.Record(ticket.Id, userId, ActivityType.Assigned, now, nameof(Ticket.AssigneeId), previous?.ToString(), request.AssigneeId.ToString(), _currentUser.CorrelationId));
            await _notifications.NotifyAssignedAsync(ticket, request.AssigneeId.Value, cancellationToken);
            changed = true;
        }

        if (request.Tags is not null)
        {
            RequirePermission(Permissions.TicketsUpdateCategory);
            ticket.TicketTags.Clear();
            await ApplyTagsAsync(ticket, request.Tags, now, cancellationToken);
            _dbContext.TicketActivities.Add(TicketActivity.Record(ticket.Id, userId, ActivityType.TagAdded, now, nameof(Ticket.TicketTags), null, string.Join(", ", request.Tags), _currentUser.CorrelationId));
            changed = true;
        }

        if (!changed)
        {
            return await GetAsync(ticket.Id, cancellationToken);
        }

        ticket.Touch(now);
        await SaveChangesAsync(cancellationToken);
        return await GetAsync(ticket.Id, cancellationToken);
    }
}
