using SupportDesk.Domain.Common;

namespace SupportDesk.Domain.Tickets;

public sealed class Category : Entity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
