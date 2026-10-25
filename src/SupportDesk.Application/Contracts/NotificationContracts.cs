using SupportDesk.Domain;

namespace SupportDesk.Application.Contracts;

public sealed record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Title,
    string? Body,
    string? Link,
    bool IsRead,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt);

