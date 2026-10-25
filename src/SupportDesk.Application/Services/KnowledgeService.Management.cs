using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Contracts;
using SupportDesk.Domain;
using SupportDesk.Domain.Common;
using SupportDesk.Domain.Knowledge;
using SupportDesk.Domain.Permissions;

namespace SupportDesk.Application.Services;

public sealed partial class KnowledgeService
{
    public async Task<KbArticleDto> PublishAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = RequireAuthenticated();
        RequirePermission(Permissions.KnowledgePublish);
        var article = await LoadAsync(id, cancellationToken);
        article.Publish(userId, _clock.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(userId, "knowledge.publish", nameof(KbArticle), article.Id.ToString(), new { article.Title }, cancellationToken);
        return await GetAsync(article.Id, cancellationToken);
    }

    public async Task<KbArticleDto> ArchiveAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = RequireAuthenticated();
        RequirePermission(Permissions.KnowledgeArchive);
        var article = await LoadAsync(id, cancellationToken);
        article.Archive(_clock.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(userId, "knowledge.archive", nameof(KbArticle), article.Id.ToString(), new { article.Title }, cancellationToken);
        return await GetAsync(article.Id, cancellationToken);
    }

    private async Task<KbArticle> LoadAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.KbArticles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Knowledge article not found.");
    }

    private void EnsureCanRead(KbArticle article)
    {
        if (_currentUser.HasPermission(Permissions.KnowledgeViewAll))
        {
            return;
        }

        RequirePermission(Permissions.KnowledgeViewPublished);
        if (article.Status != KbArticleStatus.Published || article.Visibility != KbVisibility.Customer)
        {
            throw new ForbiddenException("You do not have access to this article.");
        }
    }

    private Guid RequireAuthenticated()
    {
        return _currentUser.UserId ?? throw new ForbiddenException("Authentication is required.");
    }

    private void RequirePermission(string permission)
    {
        if (!_currentUser.HasPermission(permission))
        {
            throw new ForbiddenException($"Missing permission '{permission}'.");
        }
    }

    private static KbArticleDto Map(KbArticle article, string authorName)
    {
        return new KbArticleDto(
            article.Id,
            article.Slug,
            article.Title,
            article.Summary,
            article.Body,
            article.Status,
            article.Visibility,
            article.AuthorId,
            authorName,
            article.PublisherId,
            article.PublishedAt,
            article.CreatedAt,
            article.UpdatedAt,
            article.RowVersion);
    }
}
