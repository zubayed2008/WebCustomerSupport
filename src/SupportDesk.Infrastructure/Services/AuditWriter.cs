using System.Text.Json;
using SupportDesk.Application.Abstractions;
using SupportDesk.Domain.Auditing;

namespace SupportDesk.Infrastructure.Services;

public sealed class AuditWriter : IAuditWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAppDbContext _dbContext;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;

    public AuditWriter(IAppDbContext dbContext, IClock clock, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task WriteAsync(
        Guid? actorId,
        string action,
        string entityType,
        string entityId,
        object? details,
        CancellationToken cancellationToken)
    {
        _dbContext.AuditLogs.Add(AuditLog.Record(
            actorId,
            action,
            entityType,
            entityId,
            details is null ? null : JsonSerializer.Serialize(details, JsonOptions),
            _currentUser.IpAddress,
            _currentUser.CorrelationId,
            _clock.UtcNow));

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
