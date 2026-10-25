using System.Diagnostics;
using System.Text;
using SupportDesk.Application.Abstractions;

namespace SupportDesk.Infrastructure.Ai;

public sealed class AiOptions
{
    public string Provider { get; set; } = "Fake";

    public string? BaseUrl { get; set; }

    public string? ApiKey { get; set; }

    public string? Model { get; set; }

    public int TimeoutSeconds { get; set; } = 30;

    public int MaxConversationCharacters { get; set; } = 12000;
}

/// <summary>
/// Deterministic provider used for development, tests, and offline demos.
/// It never invents content beyond the authorized citations it is given.
/// </summary>
public sealed class FakeAiProvider : IAiProvider
{
    public string Name => "Fake";

    public Task<AiProviderResult> GenerateAsync(AiProviderRequest request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var builder = new StringBuilder();
        builder.AppendLine("AI ASSISTIVE OUTPUT - HUMAN REVIEW REQUIRED");
        builder.AppendLine();

        switch (request.Kind)
        {
            case "Classification":
                builder.AppendLine("Suggested classification:");
                builder.AppendLine("- Review the subject and description for the affected product area.");
                builder.AppendLine("- Suggested priority: High when a customer is blocked; otherwise Normal.");
                builder.AppendLine("- Confirm the category before saving.");
                break;
            case "Summary":
                builder.AppendLine("Conversation summary:");
                builder.AppendLine(Summarize(request.Conversation));
                break;
            case "KnowledgeRetrieval":
                builder.AppendLine(request.AuthorizedCitations.Count == 0
                    ? "No published knowledge article matched this ticket. Do not invent an answer."
                    : "Relevant knowledge articles:");
                foreach (var citation in request.AuthorizedCitations)
                {
                    builder.AppendLine($"- {citation.Title} ({citation.Slug}): {citation.Snippet}");
                }

                break;
            default:
                builder.AppendLine("Draft reply (edit before sending):");
                builder.AppendLine($"Hello, thank you for contacting support about \"{request.Subject}\".");
                if (request.AuthorizedCitations.Count > 0)
                {
                    builder.AppendLine();
                    builder.AppendLine("Based on our published guidance:");
                    builder.AppendLine(request.AuthorizedCitations[0].Snippet);
                    builder.AppendLine();
                    builder.AppendLine($"Reference: {request.AuthorizedCitations[0].Title}");
                }
                else
                {
                    builder.AppendLine();
                    builder.AppendLine("No published article matched, so a support specialist must provide the answer.");
                }

                builder.AppendLine();
                builder.AppendLine("Please reply if you need further assistance.");
                break;
        }

        stopwatch.Stop();
        return Task.FromResult(new AiProviderResult(
            builder.ToString().Trim(),
            request.AuthorizedCitations.Take(3).ToList(),
            Name,
            "deterministic-demo",
            stopwatch.ElapsedMilliseconds));
    }

    private static string Summarize(string conversation)
    {
        var lines = conversation
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(12)
            .Select(line => line.Length > 180 ? line[..180] : line);

        return string.Join(Environment.NewLine, lines);
    }
}
