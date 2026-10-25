using SupportDesk.Domain.Common;

namespace SupportDesk.Domain.Notifications;

public sealed class Notification : Entity
{
    public Guid RecipientId { get; set; }

    public NotificationType Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Body { get; set; }

    public string? Link { get; set; }

    public bool IsRead { get; set; }

    public DateTimeOffset? ReadAt { get; set; }

    public void MarkRead(DateTimeOffset now)
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAt = now;
            UpdatedAt = now;
        }
    }
}
