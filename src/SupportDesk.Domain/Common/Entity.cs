namespace SupportDesk.Domain.Common;

/// <summary>
/// Base type for persisted domain entities.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
