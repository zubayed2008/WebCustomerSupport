using SupportDesk.Domain.Common;

namespace SupportDesk.Domain.Tickets;

/// <summary>
/// Single source of truth for ticket status transitions.
/// </summary>
public static class TicketWorkflow
{
    private static readonly IReadOnlyDictionary<TicketStatus, HashSet<TicketStatus>> Allowed =
        new Dictionary<TicketStatus, HashSet<TicketStatus>>
        {
            [TicketStatus.New] = [TicketStatus.Open, TicketStatus.PendingInternal, TicketStatus.Resolved],
            [TicketStatus.Open] =
            [
                TicketStatus.PendingCustomer,
                TicketStatus.PendingInternal,
                TicketStatus.Resolved,
                TicketStatus.Closed,
            ],
            [TicketStatus.PendingCustomer] =
            [
                TicketStatus.Open,
                TicketStatus.PendingInternal,
                TicketStatus.Resolved,
                TicketStatus.Closed,
            ],
            [TicketStatus.PendingInternal] =
            [
                TicketStatus.Open,
                TicketStatus.PendingCustomer,
                TicketStatus.Resolved,
                TicketStatus.Closed,
            ],
            [TicketStatus.Resolved] = [TicketStatus.Open, TicketStatus.Closed],
            [TicketStatus.Closed] = [TicketStatus.Open],
        };

    public static bool IsActive(TicketStatus status)
    {
        return status is not (TicketStatus.Resolved or TicketStatus.Closed);
    }

    public static bool CanTransition(TicketStatus from, TicketStatus to)
    {
        if (from == to)
        {
            return false;
        }

        return Allowed.TryGetValue(from, out var targets) && targets.Contains(to);
    }

    public static void EnsureTransition(TicketStatus from, TicketStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new ConflictException($"Ticket cannot move from {from} to {to}.");
        }
    }

    public static bool IsReopen(TicketStatus from, TicketStatus to)
    {
        return to == TicketStatus.Open && from is TicketStatus.Resolved or TicketStatus.Closed;
    }
}
