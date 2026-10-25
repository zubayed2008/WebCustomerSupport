using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts;
using SupportDesk.Domain;

namespace SupportDesk.Application.Services;

public sealed partial class ReportService
{
    private async Task<ReportOverviewDto> BuildOverviewAsync(
        List<ActiveTicketRow> active,
        List<ResolvedRow> resolvedRows,
        List<TrendRow> trendRows,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var byStatus = Enum.GetValues<TicketStatus>()
            .Select(status => new QueueMetricDto(status.ToString(), active.Count(x => x.Status == status)))
            .ToList();
        var byPriority = Enum.GetValues<TicketPriority>()
            .Select(priority => new QueueMetricDto(priority.ToString(), active.Count(x => x.Priority == priority)))
            .ToList();

        var assigneeIds = active.Where(x => x.AssigneeId is not null).Select(x => x.AssigneeId!.Value).Distinct().ToList();
        var names = await _users.GetDisplayNamesAsync(assigneeIds, cancellationToken);
        var workload = active
            .GroupBy(x => x.AssigneeId)
            .Select(group => new AgentWorkloadDto(
                group.Key,
                group.Key is null ? "Unassigned" : names.GetValueOrDefault(group.Key.Value, "Unknown"),
                group.Count(),
                group.Count(x => x.DueSoon),
                group.Count(x => x.Overdue)))
            .OrderByDescending(x => x.ActiveTickets)
            .ToList();

        var trend = Enumerable.Range(0, 14)
            .Select(offset => DateOnly.FromDateTime(now.AddDays(-13 + offset).UtcDateTime))
            .Select(date => new TrendPointDto(
                date,
                trendRows.Count(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) == date),
                trendRows.Count(x => x.ResolvedAt is not null && DateOnly.FromDateTime(x.ResolvedAt.Value.UtcDateTime) == date)))
            .ToList();

        var responseSamples = resolvedRows.Where(x => x.FirstRespondedAt is not null)
            .Select(x => (x.FirstRespondedAt!.Value - x.CreatedAt).TotalMinutes)
            .ToList();
        var resolutionSamples = resolvedRows.Where(x => x.ResolvedAt is not null)
            .Select(x => Math.Max(0, (x.ResolvedAt!.Value - x.CreatedAt).TotalMinutes - x.PausedMinutes))
            .ToList();

        return new ReportOverviewDto(
            active.Count,
            active.Count(x => x.AssigneeId is null),
            active.Count(x => x.DueSoon),
            active.Count(x => x.Overdue),
            resolvedRows.Count,
            responseSamples.Count == 0 ? 0 : Math.Round(responseSamples.Average(), 1),
            resolutionSamples.Count == 0 ? 0 : Math.Round(resolutionSamples.Average(), 1),
            byStatus,
            byPriority,
            workload,
            trend);
    }

    private static bool IsOverdue(DateTimeOffset? firstRespondedAt, DateTimeOffset? firstResponseDueAt, DateTimeOffset? resolutionDueAt, DateTimeOffset now)
    {
        return (firstRespondedAt is null && firstResponseDueAt is not null && firstResponseDueAt < now)
            || (resolutionDueAt is not null && resolutionDueAt < now);
    }

    private static bool IsDueSoon(DateTimeOffset? firstRespondedAt, DateTimeOffset? firstResponseDueAt, DateTimeOffset? resolutionDueAt, int dueSoonMinutes, DateTimeOffset now)
    {
        var warning = now.AddMinutes(Math.Max(1, dueSoonMinutes));
        return (firstRespondedAt is null && firstResponseDueAt is not null && firstResponseDueAt <= warning && firstResponseDueAt >= now)
            || (resolutionDueAt is not null && resolutionDueAt <= warning && resolutionDueAt >= now);
    }
}
