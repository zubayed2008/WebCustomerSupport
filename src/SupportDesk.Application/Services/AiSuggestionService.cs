using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts;
using SupportDesk.Domain;
using SupportDesk.Domain.Ai;
using SupportDesk.Domain.Common;
using SupportDesk.Domain.Permissions;
using SupportDesk.Domain.Tickets;

namespace SupportDesk.Application.Services;

public sealed class AiSuggestionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAppDbContext _dbContext;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;

    public AiSuggestionService(IAppDbContext dbContext, IClock clock, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<AiSuggestionDto> RequestAsync(Guid ticketId, AiSuggestionKind kind, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException("Authentication is required.");
        RequirePermission(Permissions.AiUse);
        var ticket = await LoadAuthorizedTicketAsync(ticketId, cancellationToken);
        var revision = ComputeRevision(ticket);

        var existing = await _dbContext.AiSuggestions
            .Where(x => x.TicketId == ticketId
                && x.Kind == kind
                && x.InputRevision == revision
                && (x.Status == AiSuggestionStatus.Queued || x.Status == AiSuggestionStatus.Running || x.Status == AiSuggestionStatus.Completed))
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            return Map(existing);
        }

        var now = _clock.UtcNow;
        var suggestion = new AiSuggestion
        {
            TicketId = ticketId,
            RequestedById = userId,
            Kind = kind,
            Status = AiSuggestionStatus.Queued,
            InputRevision = revision,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _dbContext.AiSuggestions.Add(suggestion);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Map(suggestion);
    }

    public async Task<IReadOnlyList<AiSuggestionDto>> ListAsync(Guid ticketId, CancellationToken cancellationToken)
    {
        RequirePermission(Permissions.AiUse);
        _ = await LoadAuthorizedTicketAsync(ticketId, cancellationToken);
        var suggestions = await _dbContext.AiSuggestions
            .AsNoTracking()
            .Where(x => x.TicketId == ticketId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        return suggestions.Select(Map).ToList();
    }

    public async Task<AiSuggestionDto> AddFeedbackAsync(Guid id, AiFeedbackRequest request, CancellationToken cancellationToken)
    {
        RequirePermission(Permissions.AiReview);
        var suggestion = await _dbContext.AiSuggestions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("AI suggestion not found.");
        _ = await LoadAuthorizedTicketAsync(suggestion.TicketId, cancellationToken);

        suggestion.AddFeedback(request.Rating, request.Comment, _clock.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Map(suggestion);
    }

    private async Task<Ticket> LoadAuthorizedTicketAsync(Guid ticketId, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException("Authentication is required.");
        var ticket = await _dbContext.Tickets
            .Include(x => x.Comments)
            .FirstOrDefaultAsync(x => x.Id == ticketId, cancellationToken)
            ?? throw new NotFoundException("Ticket not found.");

        if (!_currentUser.HasPermission(Permissions.TicketsViewAll) && ticket.RequesterId != userId)
        {
            throw new ForbiddenException("You do not have access to this ticket.");
        }

        return ticket;
    }

    private void RequirePermission(string permission)
    {
        if (!_currentUser.HasPermission(permission))
        {
            throw new ForbiddenException($"Missing permission '{permission}'.");
        }
    }

    public static string ComputeRevision(Ticket ticket)
    {
        var lastComment = ticket.Comments.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
        var value = $"{ticket.Id:N}:{ticket.UpdatedAt.UtcTicks}:{ticket.Status}:{ticket.Priority}:{ticket.Comments.Count}:{lastComment?.CreatedAt.UtcTicks ?? 0}";
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)))[..32];
    }

    public static AiSuggestionDto Map(AiSuggestion suggestion)
    {
        var citations = string.IsNullOrWhiteSpace(suggestion.CitationsJson)
            ? []
            : JsonSerializer.Deserialize<List<AiCitationDto>>(suggestion.CitationsJson, JsonOptions) ?? [];

        return new AiSuggestionDto(
            suggestion.Id,
            suggestion.TicketId,
            suggestion.Kind,
            suggestion.Status,
            suggestion.InputRevision,
            suggestion.OutputText,
            citations,
            suggestion.Error,
            suggestion.Provider,
            suggestion.Model,
            suggestion.PromptVersion,
            suggestion.LatencyMs,
            suggestion.FeedbackRating,
            suggestion.CreatedAt,
            suggestion.CompletedAt);
    }
}
