namespace SupportDesk.Application.Contracts;

public sealed record ReportOverviewDto(
    int OpenTickets,
    int UnassignedTickets,
    int DueSoonTickets,
    int OverdueTickets,
    int ResolvedInPeriod,
    double AverageFirstResponseMinutes,
    double AverageResolutionMinutes,
    IReadOnlyList<QueueMetricDto> ByStatus,
    IReadOnlyList<QueueMetricDto> ByPriority,
    IReadOnlyList<AgentWorkloadDto> AgentWorkload,
    IReadOnlyList<TrendPointDto> VolumeTrend);

public sealed record QueueMetricDto(string Label, int Count);

public sealed record AgentWorkloadDto(
    Guid? AgentId,
    string AgentName,
    int ActiveTickets,
    int DueSoon,
    int Overdue);

public sealed record TrendPointDto(DateOnly Date, int Created, int Resolved);

