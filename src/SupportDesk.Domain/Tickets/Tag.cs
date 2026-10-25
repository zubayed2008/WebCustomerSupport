using SupportDesk.Domain.Common;

namespace SupportDesk.Domain.Tickets;

public sealed class Tag : Entity
{
    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public ICollection<TicketTag> TicketTags { get; set; } = [];

    public static string Normalize(string name)
    {
        return name.Trim().ToLowerInvariant();
    }
}

public sealed class TicketTag
{
    public Guid TicketId { get; set; }

    public Ticket? Ticket { get; set; }

    public Guid TagId { get; set; }

    public Tag? Tag { get; set; }
}
