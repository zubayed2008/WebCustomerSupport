using SupportDesk.Domain.Common;
using SupportDesk.Domain.Tickets;

namespace SupportDesk.Domain.Knowledge;

public sealed class KbArticle : Entity
{
    public string Slug { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public KbArticleStatus Status { get; set; } = KbArticleStatus.Draft;

    public KbVisibility Visibility { get; set; } = KbVisibility.Internal;

    public Guid AuthorId { get; set; }

    public Guid? PublisherId { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public uint RowVersion { get; set; }

    public ICollection<KbArticleRevision> Revisions { get; set; } = [];

    public ICollection<ArticleTicketLink> TicketLinks { get; set; } = [];

    public static KbArticle Create(
        string slug,
        string title,
        string summary,
        string body,
        KbVisibility visibility,
        Guid authorId,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ValidationFailedException("Article title is required.");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ValidationFailedException("Article body is required.");
        }

        return new KbArticle
        {
            Slug = string.IsNullOrWhiteSpace(slug) ? CreateSlug(title) : CreateSlug(slug),
            Title = title.Trim(),
            Summary = summary.Trim(),
            Body = body.Trim(),
            Visibility = visibility,
            AuthorId = authorId,
            Status = KbArticleStatus.Draft,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void Publish(Guid publisherId, DateTimeOffset now)
    {
        if (Status == KbArticleStatus.Archived)
        {
            throw new ConflictException("Archived articles cannot be published. Create a new revision instead.");
        }

        Status = KbArticleStatus.Published;
        PublisherId = publisherId;
        PublishedAt ??= now;
        UpdatedAt = now;
    }

    public void Archive(DateTimeOffset now)
    {
        Status = KbArticleStatus.Archived;
        UpdatedAt = now;
    }

    public void UpdateContent(string title, string summary, string body, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body))
        {
            throw new ValidationFailedException("Article title and body are required.");
        }

        Title = title.Trim();
        Summary = summary.Trim();
        Body = body.Trim();
        UpdatedAt = now;

        if (Status == KbArticleStatus.Published)
        {
            Status = KbArticleStatus.Draft;
            PublishedAt = null;
        }
    }

    public static string CreateSlug(string value)
    {
        var chars = value.Trim().ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray();
        var slug = new string(chars);

        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        slug = slug.Trim('-');
        return string.IsNullOrEmpty(slug) ? $"article-{Guid.NewGuid():N}"[..20] : slug[..Math.Min(slug.Length, 160)];
    }
}

public sealed class KbArticleRevision : Entity
{
    public Guid ArticleId { get; set; }

    public KbArticle? Article { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public Guid EditorId { get; set; }
}

public sealed class ArticleTicketLink
{
    public Guid ArticleId { get; set; }

    public KbArticle? Article { get; set; }

    public Guid TicketId { get; set; }

    public Ticket? Ticket { get; set; }
}
