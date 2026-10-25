using SupportDesk.Domain.Common;

namespace SupportDesk.Domain.Tickets;

/// <summary>
/// Append-only audit record for ticket changes. Rows are never updated.
/// </summary>
public sealed class TicketActivity : Entity
{
    public Guid TicketId { get; set; }

    public Ticket? Ticket { get; set; }

    public Guid? ActorId { get; set; }

    public ActivityType Type { get; set; }

    public string? Field { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public string? CorrelationId { get; set; }

    public static TicketActivity Record(
        Guid ticketId,
        Guid? actorId,
        ActivityType type,
        DateTimeOffset now,
        string? field = null,
        string? oldValue = null,
        string? newValue = null,
        string? correlationId = null)
    {
        return new TicketActivity
        {
            TicketId = ticketId,
            ActorId = actorId,
            Type = type,
            Field = field,
            OldValue = oldValue,
            NewValue = newValue,
            CorrelationId = correlationId,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }
}
