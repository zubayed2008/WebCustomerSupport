using SupportDesk.Domain;

namespace SupportDesk.Application.Contracts;

public sealed record AiSuggestionDto(
    Guid Id,
    Guid TicketId,
    AiSuggestionKind Kind,
    AiSuggestionStatus Status,
    string InputRevision,
    string? OutputText,
    IReadOnlyList<AiCitationDto> Citations,
    string? Error,
    string Provider,
    string? Model,
    string PromptVersion,
    long? LatencyMs,
    int? FeedbackRating,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

public sealed record AiCitationDto(Guid ArticleId, string Title, string Slug, string Snippet);

public sealed record RequestAiSuggestionRequest(AiSuggestionKind Kind);

public sealed record AiFeedbackRequest(int Rating, string? Comment);

