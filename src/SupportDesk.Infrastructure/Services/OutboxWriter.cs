using System.Text.Json;
using SupportDesk.Application.Abstractions;
using SupportDesk.Domain;
using SupportDesk.Domain.Outbox;

namespace SupportDesk.Infrastructure.Services;

/// <summary>
/// Adds outbox rows to the current unit of work. The caller's SaveChanges
/// commits the business change and the outbox row in one transaction.
/// </summary>
public sealed class OutboxWriter : IOutboxWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAppDbContext _dbContext;
    private readonly IClock _clock;

    public OutboxWriter(IAppDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public Task EnqueueAsync(string type, object payload, string idempotencyKey, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        _dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Type = type,
            PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
            Status = OutboxStatus.Pending,
            Attempts = 0,
            NextAttemptAt = now,
            IdempotencyKey = idempotencyKey,
            CreatedAt = now,
            UpdatedAt = now,
        });

        return Task.CompletedTask;
    }
}
