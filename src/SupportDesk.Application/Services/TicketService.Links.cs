using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Contracts;
using SupportDesk.Domain;
using SupportDesk.Domain.Common;
using SupportDesk.Domain.Knowledge;
using SupportDesk.Domain.Permissions;
using SupportDesk.Domain.Tickets;

namespace SupportDesk.Application.Services;

public sealed partial class TicketService
{
    public async Task<TicketDetailDto> LinkArticleAsync(Guid ticketId, Guid articleId, CancellationToken cancellationToken)
    {
        var userId = RequireAuthenticated();
        RequirePermission(Permissions.KnowledgeViewPublished);
        var ticket = await LoadDetailAsync(ticketId, cancellationToken);
        await EnsureCanReadAsync(ticket, cancellationToken);

        var article = await _dbContext.KbArticles.FirstOrDefaultAsync(x => x.Id == articleId, cancellationToken)
            ?? throw new NotFoundException("Knowledge article not found.");

        var canViewAll = _currentUser.HasPermission(Permissions.KnowledgeViewAll);
        if (!canViewAll && (article.Status != KbArticleStatus.Published || article.Visibility != KbVisibility.Customer))
        {
            throw new ForbiddenException("You do not have access to this article.");
        }

        if (ticket.ArticleLinks.All(x => x.ArticleId != articleId))
        {
            ticket.ArticleLinks.Add(new ArticleTicketLink { TicketId = ticket.Id, ArticleId = articleId, Article = article });
        }

        var now = _clock.UtcNow;
        ticket.Touch(now);
        _dbContext.TicketActivities.Add(TicketActivity.Record(ticket.Id, userId, ActivityType.ArticleLinked, now, "Article", null, article.Title, _currentUser.CorrelationId));
        await SaveChangesAsync(cancellationToken);
        return await GetAsync(ticket.Id, cancellationToken);
    }

    public async Task<TicketDetailDto> UnlinkArticleAsync(Guid ticketId, Guid articleId, CancellationToken cancellationToken)
    {
        var userId = RequireAuthenticated();
        RequirePermission(Permissions.KnowledgeViewPublished);
        var ticket = await LoadDetailAsync(ticketId, cancellationToken);
        await EnsureCanReadAsync(ticket, cancellationToken);

        var link = ticket.ArticleLinks.FirstOrDefault(x => x.ArticleId == articleId);
        if (link is not null)
        {
            ticket.ArticleLinks.Remove(link);
            var now = _clock.UtcNow;
            ticket.Touch(now);
            _dbContext.TicketActivities.Add(TicketActivity.Record(ticket.Id, userId, ActivityType.ArticleLinked, now, "Article", link.Article?.Title, null, _currentUser.CorrelationId));
            await SaveChangesAsync(cancellationToken);
        }

        return await GetAsync(ticket.Id, cancellationToken);
    }
}
