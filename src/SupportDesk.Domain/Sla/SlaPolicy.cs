using SupportDesk.Domain.Common;

namespace SupportDesk.Domain.Sla;

public sealed class SlaPolicy : Entity
{
    public string Name { get; set; } = string.Empty;

    public TicketPriority Priority { get; set; }

    public int FirstResponseMinutes { get; set; }

    public int ResolutionMinutes { get; set; }

    public int DueSoonMinutes { get; set; } = 60;

    public int Version { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public Guid BusinessCalendarId { get; set; }

    public BusinessCalendar? BusinessCalendar { get; set; }
}

public sealed record SlaCommitment(
    Guid PolicyId,
    DateTimeOffset FirstResponseDueAt,
    DateTimeOffset ResolutionDueAt);
