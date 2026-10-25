using SupportDesk.Domain.Common;

namespace SupportDesk.Domain.Outbox;

public sealed class OutboxMessage : Entity
{
    public string Type { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = "{}";

    public OutboxStatus Status { get; set; } = OutboxStatus.Pending;

    public int Attempts { get; set; }

    public DateTimeOffset NextAttemptAt { get; set; }

    public DateTimeOffset? LockedUntil { get; set; }

    public DateTimeOffset? ProcessedAt { get; set; }

    public string? LastError { get; set; }

    public string IdempotencyKey { get; set; } = string.Empty;
}
