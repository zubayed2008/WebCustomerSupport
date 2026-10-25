using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts;
using SupportDesk.Domain;
using SupportDesk.Domain.Ai;
using SupportDesk.Domain.Permissions;

namespace SupportDesk.Application.Services;

/// <summary>
/// Processes queued AI suggestions. Runs outside the HTTP request so slow or
/// failing providers cannot block ticket handling.
/// </summary>
public sealed partial class AiSuggestionProcessor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAppDbContext _dbContext;
    private readonly IClock _clock;
    private readonly IAiProvider _provider;
    private readonly IPermissionService _permissions;

    public AiSuggestionProcessor(IAppDbContext dbContext, IClock clock, IAiProvider provider, IPermissionService permissions)
    {
        _dbContext = dbContext;
        _clock = clock;
        _provider = provider;
        _permissions = permissions;
    }

    public async Task<int> ProcessBatchAsync(int maxItems, CancellationToken cancellationToken)
    {
        var suggestions = await _dbContext.AiSuggestions
            .Where(x => x.Status == AiSuggestionStatus.Queued)
            .OrderBy(x => x.CreatedAt)
            .Take(Math.Clamp(maxItems, 1, 50))
            .ToListAsync(cancellationToken);

        var processed = 0;
        foreach (var suggestion in suggestions)
        {
            await ProcessOneAsync(suggestion, cancellationToken);
            processed++;
        }

        return processed;
    }

    private async Task ProcessOneAsync(AiSuggestion suggestion, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        try
        {
            var ticket = await _dbContext.Tickets
                .Include(x => x.Comments)
                .FirstOrDefaultAsync(x => x.Id == suggestion.TicketId, cancellationToken);

            if (ticket is null)
            {
                suggestion.MarkFailed("Ticket no longer exists.", now);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            if (AiSuggestionService.ComputeRevision(ticket) != suggestion.InputRevision)
            {
                suggestion.MarkStale(now);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            suggestion.MarkRunning(now);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var canViewInternal = await _permissions.UserHasPermissionAsync(suggestion.RequestedById, Permissions.KnowledgeViewAll, cancellationToken);
            var citations = await FindCitationsAsync(ticket.Subject, ticket.Description, canViewInternal, cancellationToken);
            var conversation = BuildConversation(ticket, canViewInternal);

            var result = await _provider.GenerateAsync(
                new AiProviderRequest(suggestion.Kind.ToString(), ticket.Subject, conversation, citations, suggestion.PromptVersion),
                cancellationToken);

            var usedCitations = result.UsedCitations.Select(x => new AiCitationDto(x.ArticleId, x.Title, x.Slug, x.Snippet)).ToList();
            suggestion.MarkCompleted(
                result.Output,
                usedCitations.Count == 0 ? null : JsonSerializer.Serialize(usedCitations, JsonOptions),
                result.Provider,
                result.Model,
                result.LatencyMs,
                _clock.UtcNow);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var message = exception.Message.Length > 500 ? exception.Message[..500] : exception.Message;
            suggestion.MarkFailed(message, _clock.UtcNow);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
