using SupportDesk.Domain.Common;

namespace SupportDesk.Domain.Ai;

public sealed class AiSuggestion : Entity
{
    public Guid TicketId { get; set; }

    public Guid RequestedById { get; set; }

    public AiSuggestionKind Kind { get; set; }

    public AiSuggestionStatus Status { get; set; } = AiSuggestionStatus.Queued;

    public string InputRevision { get; set; } = string.Empty;

    public string? OutputText { get; set; }

    public string? CitationsJson { get; set; }

    public string? Error { get; set; }

    public string Provider { get; set; } = "Fake";

    public string? Model { get; set; }

    public string PromptVersion { get; set; } = "v1";

    public long? LatencyMs { get; set; }

    public int? FeedbackRating { get; set; }

    public string? FeedbackComment { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public void MarkRunning(DateTimeOffset now)
    {
        Status = AiSuggestionStatus.Running;
        UpdatedAt = now;
    }

    public void MarkCompleted(string output, string? citationsJson, string provider, string? model, long latencyMs, DateTimeOffset now)
    {
        OutputText = output;
        CitationsJson = citationsJson;
        Provider = provider;
        Model = model;
        LatencyMs = latencyMs;
        Status = AiSuggestionStatus.Completed;
        CompletedAt = now;
        UpdatedAt = now;
        Error = null;
    }

    public void MarkFailed(string error, DateTimeOffset now)
    {
        Error = error;
        Status = AiSuggestionStatus.Failed;
        CompletedAt = now;
        UpdatedAt = now;
    }

    public void MarkStale(DateTimeOffset now)
    {
        Status = AiSuggestionStatus.Stale;
        UpdatedAt = now;
    }

    public void AddFeedback(int rating, string? comment, DateTimeOffset now)
    {
        if (rating is < 1 or > 5)
        {
            throw new ValidationFailedException("Feedback rating must be between 1 and 5.");
        }

        FeedbackRating = rating;
        FeedbackComment = comment;
        UpdatedAt = now;
    }
}
