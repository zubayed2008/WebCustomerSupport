using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts;
using SupportDesk.Domain;
using SupportDesk.Domain.Common;
using SupportDesk.Domain.Knowledge;
using SupportDesk.Domain.Permissions;

namespace SupportDesk.Application.Services;

public sealed partial class KnowledgeService
{
    private readonly IAppDbContext _dbContext;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly IUserDirectory _users;
    private readonly IAuditWriter _audit;

    public KnowledgeService(
        IAppDbContext dbContext,
        IClock clock,
        ICurrentUser currentUser,
        IUserDirectory users,
        IAuditWriter audit)
    {
        _dbContext = dbContext;
        _clock = clock;
        _currentUser = currentUser;
        _users = users;
        _audit = audit;
    }

    public async Task<PagedResult<KbArticleListItemDto>> ListAsync(KbArticleQuery query, CancellationToken cancellationToken)
    {
        RequirePermission(Permissions.KnowledgeViewPublished);
        var canViewAll = _currentUser.HasPermission(Permissions.KnowledgeViewAll);

        var articles = _dbContext.KbArticles.AsNoTracking().AsQueryable();
        if (!canViewAll)
        {
            articles = articles.Where(x => x.Status == KbArticleStatus.Published && x.Visibility == KbVisibility.Customer);
        }
        else if (query.Status is not null)
        {
            articles = articles.Where(x => x.Status == query.Status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            if (term.Length > 100)
            {
                term = term[..100];
            }

            // PostgreSQL full-text search with a web-style query parser. The term is
            // passed as a parameter, never concatenated into SQL.
            articles = articles.Where(x => EF.Functions
                .ToTsVector("english", x.Title + " " + (x.Summary ?? string.Empty) + " " + x.Body)
                .Matches(EF.Functions.WebSearchToTsQuery("english", term)));
        }

        var page = Math.Clamp(query.Page, 1, 10000);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var total = await articles.CountAsync(cancellationToken);
        var rows = await articles
            .OrderByDescending(x => x.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var names = await _users.GetDisplayNamesAsync(rows.Select(x => x.AuthorId), cancellationToken);
        var items = rows.Select(x => new KbArticleListItemDto(
            x.Id,
            x.Slug,
            x.Title,
            x.Summary,
            x.Status,
            x.Visibility,
            names.GetValueOrDefault(x.AuthorId, "Unknown"),
            x.UpdatedAt,
            x.PublishedAt)).ToList();

        return new PagedResult<KbArticleListItemDto>(items, page, pageSize, total);
    }

    public async Task<KbArticleDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var article = await LoadAsync(id, cancellationToken);
        EnsureCanRead(article);
        var names = await _users.GetDisplayNamesAsync([article.AuthorId], cancellationToken);
        return Map(article, names.GetValueOrDefault(article.AuthorId, "Unknown"));
    }

    public async Task<KbArticleDto> CreateAsync(CreateKbArticleRequest request, CancellationToken cancellationToken)
    {
        var userId = RequireAuthenticated();
        RequirePermission(Permissions.KnowledgeCreate);
        var now = _clock.UtcNow;

        var article = KbArticle.Create(request.Slug ?? request.Title, request.Title, request.Summary, request.Body, request.Visibility, userId, now);
        _dbContext.KbArticles.Add(article);
        _dbContext.KbArticleRevisions.Add(new KbArticleRevision
        {
            ArticleId = article.Id,
            Title = article.Title,
            Body = article.Body,
            EditorId = userId,
            CreatedAt = now,
            UpdatedAt = now,
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(userId, "knowledge.create", nameof(KbArticle), article.Id.ToString(), new { article.Title }, cancellationToken);
        return await GetAsync(article.Id, cancellationToken);
    }

    public async Task<KbArticleDto> UpdateAsync(Guid id, UpdateKbArticleRequest request, CancellationToken cancellationToken)
    {
        var userId = RequireAuthenticated();
        RequirePermission(Permissions.KnowledgeCreate);
        var article = await LoadAsync(id, cancellationToken);
        EnsureCanRead(article);
        var now = _clock.UtcNow;

        article.UpdateContent(request.Title, request.Summary, request.Body, now);
        article.Visibility = request.Visibility;
        _dbContext.KbArticleRevisions.Add(new KbArticleRevision
        {
            ArticleId = article.Id,
            Title = article.Title,
            Body = article.Body,
            EditorId = userId,
            CreatedAt = now,
            UpdatedAt = now,
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(userId, "knowledge.update", nameof(KbArticle), article.Id.ToString(), new { article.Title }, cancellationToken);
        return await GetAsync(article.Id, cancellationToken);
    }
}
