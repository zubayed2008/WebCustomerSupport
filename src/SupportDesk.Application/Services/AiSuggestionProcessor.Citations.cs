using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Abstractions;
using SupportDesk.Domain;
using SupportDesk.Domain.Knowledge;

namespace SupportDesk.Application.Services;

public sealed partial class AiSuggestionProcessor
{
    private async Task<List<AiKnowledgeCitation>> FindCitationsAsync(string subject, string description, bool includeInternal, CancellationToken cancellationToken)
    {
        var words = (subject + " " + description)
            .ToLowerInvariant()
            .Split([' ', ',', '.', ';', ':', '!', '?', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries)
            .Where(x => x.Length >= 4)
            .Distinct()
            .Take(8)
            .ToList();

        var query = _dbContext.KbArticles.AsNoTracking()
            .Where(x => x.Status == KbArticleStatus.Published);

        if (!includeInternal)
        {
            query = query.Where(x => x.Visibility == KbVisibility.Customer);
        }

        var candidates = await query.OrderByDescending(x => x.UpdatedAt).Take(50).ToListAsync(cancellationToken);
        return candidates
            .Where(article => words.Count == 0
                || words.Any(word => article.Title.ToLowerInvariant().Contains(word)
                    || article.Summary.ToLowerInvariant().Contains(word)
                    || article.Body.ToLowerInvariant().Contains(word)))
            .Take(5)
            .Select(article => new AiKnowledgeCitation(
                article.Id,
                article.Title,
                article.Slug,
                article.Summary.Length > 240 ? article.Summary[..240] : article.Summary))
            .ToList();
    }

    private static string BuildConversation(Domain.Tickets.Ticket ticket, bool includeInternal)
    {
        var comments = ticket.Comments
            .Where(x => includeInternal || x.Visibility == CommentVisibility.Public)
            .OrderBy(x => x.CreatedAt)
            .Select(x => $"{(x.Visibility == CommentVisibility.Internal ? "[internal]" : "[public]")} {x.Body}")
            .Take(30);

        var builder = new System.Text.StringBuilder();
        builder.AppendLine($"Subject: {ticket.Subject}");
        builder.AppendLine($"Description: {ticket.Description}");
        builder.AppendLine($"Status: {ticket.Status}; Priority: {ticket.Priority}");
        foreach (var comment in comments)
        {
            builder.AppendLine(comment);
        }

        return builder.ToString();
    }
}
