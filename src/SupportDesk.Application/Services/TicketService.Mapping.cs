using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Contracts;
using SupportDesk.Domain;
using SupportDesk.Domain.Knowledge;
using SupportDesk.Domain.Permissions;
using SupportDesk.Domain.Tickets;

namespace SupportDesk.Application.Services;

public sealed partial class TicketService
{
    private async Task<TicketDetailDto> MapDetailAsync(Ticket ticket, CancellationToken cancellationToken)
    {
        var canViewAll = _currentUser.HasPermission(Permissions.TicketsViewAll);
        var userIds = new List<Guid> { ticket.RequesterId };
        if (ticket.AssigneeId is not null)
        {
            userIds.Add(ticket.AssigneeId.Value);
        }

        userIds.AddRange(ticket.Comments.Select(x => x.AuthorId));
        userIds.AddRange(ticket.Activities.Where(x => x.ActorId != null).Select(x => x.ActorId!.Value));
        var names = await _users.GetDisplayNamesAsync(userIds.Distinct(), cancellationToken);

        var visibleComments = ticket.Comments
            .Where(x => canViewAll || x.Visibility == CommentVisibility.Public)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new TicketCommentDto(
                x.Id,
                x.AuthorId,
                names.GetValueOrDefault(x.AuthorId, "Unknown"),
                x.Body,
                x.Visibility,
                x.IsFirstResponse,
                x.CreatedAt,
                x.Attachments.Select(a => MapAttachment(a)).ToList()))
            .ToList();

        var visibleActivities = ticket.Activities
            .Where(x => canViewAll || x.Type != ActivityType.InternalNote)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new TicketActivityDto(
                x.Id,
                x.Type,
                x.ActorId,
                x.ActorId is null ? null : names.GetValueOrDefault(x.ActorId.Value, "Unknown"),
                x.Field,
                x.OldValue,
                x.NewValue,
                x.CreatedAt))
            .ToList();

        var visibleAttachments = ticket.Attachments
            .Where(x => canViewAll || x.CommentId is null || visibleComments.Any(c => c.Id == x.CommentId))
            .Select(MapAttachment)
            .ToList();

        var linkedArticles = ticket.ArticleLinks
            .Where(x => x.Article is not null)
            .Where(x => canViewAll
                || (x.Article!.Status == KbArticleStatus.Published && x.Article.Visibility == KbVisibility.Customer))
            .Select(x => new LinkedArticleDto(x.Article!.Id, x.Article.Title, x.Article.Slug, x.Article.Status))
            .ToList();

        var now = _clock.UtcNow;
        var dueSoonMinutes = ticket.SlaPolicy?.DueSoonMinutes ?? 60;

        return new TicketDetailDto(
            ticket.Id,
            ticket.Number,
            ticket.Subject,
            ticket.Description,
            ticket.Status,
            ticket.Priority,
            ticket.CategoryId,
            ticket.Category?.Name,
            ticket.RequesterId,
            names.GetValueOrDefault(ticket.RequesterId, "Unknown"),
            ticket.AssigneeId,
            ticket.AssigneeId is null ? null : names.GetValueOrDefault(ticket.AssigneeId.Value, "Unknown"),
            ticket.TicketTags.Select(x => x.Tag?.Name ?? string.Empty).Where(x => x.Length > 0).OrderBy(x => x).ToList(),
            ticket.CreatedAt,
            ticket.UpdatedAt,
            ticket.FirstResponseDueAt,
            ticket.ResolutionDueAt,
            ticket.FirstRespondedAt,
            ticket.ResolvedAt,
            ticket.ClosedAt,
            IsDueSoon(ticket, dueSoonMinutes, now),
            IsOverdue(ticket, now),
            ticket.ResponseBreached,
            ticket.ResolutionBreached,
            ticket.PausedMinutes,
            ticket.RowVersion,
            visibleComments,
            visibleActivities,
            visibleAttachments,
            linkedArticles);
    }

    private static TicketAttachmentDto MapAttachment(TicketAttachment attachment)
    {
        return new TicketAttachmentDto(
            attachment.Id,
            attachment.FileName,
            attachment.ContentType,
            attachment.SizeBytes,
            attachment.UploadedById,
            attachment.CreatedAt);
    }

    private async Task ApplyTagsAsync(Ticket ticket, IReadOnlyList<string> tagNames, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var normalized = tagNames
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(Tag.Normalize)
            .Distinct()
            .Take(10)
            .ToList();

        var existing = await _dbContext.Tags
            .Where(x => normalized.Contains(x.NormalizedName))
            .ToListAsync(cancellationToken);

        foreach (var name in normalized)
        {
            var tag = existing.FirstOrDefault(x => x.NormalizedName == name);
            if (tag is null)
            {
                tag = new Tag
                {
                    Name = name.Length > 60 ? name[..60] : name,
                    NormalizedName = name.Length > 60 ? name[..60] : name,
                    CreatedAt = now,
                    UpdatedAt = now,
                };
                _dbContext.Tags.Add(tag);
                existing.Add(tag);
            }

            if (ticket.TicketTags.All(x => x.TagId != tag.Id))
            {
                ticket.TicketTags.Add(new TicketTag { TicketId = ticket.Id, TagId = tag.Id, Tag = tag });
            }
        }
    }
}
