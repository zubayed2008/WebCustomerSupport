using SupportDesk.Domain.Common;

namespace SupportDesk.Domain.Auditing;

public sealed class AuditLog : Entity
{
    public Guid? ActorId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public string? DetailsJson { get; set; }

    public string? IpAddress { get; set; }

    public string? CorrelationId { get; set; }

    public static AuditLog Record(
        Guid? actorId,
        string action,
        string entityType,
        string entityId,
        string? detailsJson,
        string? ipAddress,
        string? correlationId,
        DateTimeOffset now)
    {
        return new AuditLog
        {
            ActorId = actorId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            DetailsJson = detailsJson,
            IpAddress = ipAddress,
            CorrelationId = correlationId,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }
}
