namespace SupportDesk.Application.Abstractions;

public sealed record AiKnowledgeCitation(Guid ArticleId, string Title, string Slug, string Snippet);

public sealed record AiProviderRequest(
    string Kind,
    string Subject,
    string Conversation,
    IReadOnlyList<AiKnowledgeCitation> AuthorizedCitations,
    string PromptVersion);

public sealed record AiProviderResult(
    string Output,
    IReadOnlyList<AiKnowledgeCitation> UsedCitations,
    string Provider,
    string? Model,
    long LatencyMs);

public interface IAiProvider
{
    string Name { get; }

    Task<AiProviderResult> GenerateAsync(AiProviderRequest request, CancellationToken cancellationToken);
}
