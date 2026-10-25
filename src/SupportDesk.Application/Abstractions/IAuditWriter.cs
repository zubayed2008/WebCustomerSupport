namespace SupportDesk.Application.Abstractions;

public interface IAuditWriter
{
    Task WriteAsync(
        Guid? actorId,
        string action,
        string entityType,
        string entityId,
        object? details,
        CancellationToken cancellationToken);
}
