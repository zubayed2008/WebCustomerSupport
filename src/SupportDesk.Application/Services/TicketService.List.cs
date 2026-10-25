using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Contracts;
using SupportDesk.Domain;
using SupportDesk.Domain.Common;
using SupportDesk.Domain.Permissions;

namespace SupportDesk.Application.Services;

public sealed partial class TicketService
{
    public async Task<PagedResult<TicketListItemDto>> ListAsync(TicketQuery query, CancellationToken cancellationToken)
    {
        var userId = RequireAuthenticated();
        var canViewAll = _currentUser.HasPermission(Permissions.TicketsViewAll);

        if (!canViewAll && !_currentUser.HasPermission(Permissions.TicketsViewOwn))
        {
            throw new ForbiddenException("You do not have permission to view tickets.");
        }

        var tickets = _dbContext.Tickets.AsNoTracking().AsQueryable();

        if (!canViewAll)
        {
            tickets = tickets.Where(x => x.RequesterId == userId);
        }

        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<TicketStatus>(query.Status, true, out var status))
        {
            tickets = tickets.Where(x => x.Status == status);
        }

        if (query.Priority is not null)
        {
            tickets = tickets.Where(x => x.Priority == query.Priority);
        }

        if (query.CategoryId is not null)
        {
            tickets = tickets.Where(x => x.CategoryId == query.CategoryId);
        }

        if (query.AssigneeId is not null)
        {
            tickets = tickets.Where(x => x.AssigneeId == query.AssigneeId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLowerInvariant();
            if (term.Length > 100)
            {
                term = term[..100];
            }

            tickets = tickets.Where(x =>
                x.Subject.ToLower().Contains(term)
                || x.Number.ToLower().Contains(term)
                || x.Description.ToLower().Contains(term));
        }

        var now = _clock.UtcNow;
        switch (query.Queue?.ToLowerInvariant())
        {
            case "unassigned":
                tickets = tickets.Where(x => x.AssigneeId == null && x.Status != TicketStatus.Resolved && x.Status != TicketStatus.Closed);
                break;
            case "mine":
                tickets = tickets.Where(x => x.AssigneeId == userId && x.Status != TicketStatus.Resolved && x.Status != TicketStatus.Closed);
                break;
            case "active":
                tickets = tickets.Where(x => x.Status != TicketStatus.Resolved && x.Status != TicketStatus.Closed);
                break;
            case "overdue":
                tickets = tickets.Where(x =>
                    x.Status != TicketStatus.Resolved
                    && x.Status != TicketStatus.Closed
                    && ((x.FirstRespondedAt == null && x.FirstResponseDueAt != null && x.FirstResponseDueAt < now)
                        || (x.ResolutionDueAt != null && x.ResolutionDueAt < now)));
                break;
        }

        var page = Math.Clamp(query.Page, 1, 10000);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var total = await tickets.CountAsync(cancellationToken);

        var rows = await tickets
            .OrderByDescending(x => x.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                Ticket = x,
                CategoryName = x.Category != null ? x.Category.Name : null,
                DueSoonMinutes = x.SlaPolicy != null ? x.SlaPolicy.DueSoonMinutes : 60,
                CommentCount = x.Comments.Count,
            })
            .ToListAsync(cancellationToken);

        var userIds = rows.Select(x => x.Ticket.RequesterId)
            .Concat(rows.Where(x => x.Ticket.AssigneeId != null).Select(x => x.Ticket.AssigneeId!.Value))
            .Distinct();
        var names = await _users.GetDisplayNamesAsync(userIds, cancellationToken);

        var items = rows.Select(row => new TicketListItemDto(
            row.Ticket.Id,
            row.Ticket.Number,
            row.Ticket.Subject,
            row.Ticket.Status,
            row.Ticket.Priority,
            row.CategoryName,
            row.Ticket.RequesterId,
            names.GetValueOrDefault(row.Ticket.RequesterId, "Unknown"),
            row.Ticket.AssigneeId,
            row.Ticket.AssigneeId is null ? null : names.GetValueOrDefault(row.Ticket.AssigneeId.Value, "Unknown"),
            row.Ticket.CreatedAt,
            row.Ticket.UpdatedAt,
            row.Ticket.FirstResponseDueAt,
            row.Ticket.ResolutionDueAt,
            IsDueSoon(row.Ticket, row.DueSoonMinutes, now),
            IsOverdue(row.Ticket, now),
            row.CommentCount)).ToList();

        return new PagedResult<TicketListItemDto>(items, page, pageSize, total);
    }
}
