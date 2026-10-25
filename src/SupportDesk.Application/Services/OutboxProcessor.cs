using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Abstractions;
using SupportDesk.Domain;
using SupportDesk.Domain.Outbox;

namespace SupportDesk.Application.Services;

/// <summary>
/// Dispatches outbox messages with leases, bounded retries, and dead-letter
/// visibility. Each message is idempotent by its unique idempotency key.
/// </summary>
public sealed class OutboxProcessor
{
    private const int MaxAttempts = 5;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAppDbContext _dbContext;
    private readonly IClock _clock;
    private readonly IEmailSender _emailSender;

    public OutboxProcessor(IAppDbContext dbContext, IClock clock, IEmailSender emailSender)
    {
        _dbContext = dbContext;
        _clock = clock;
        _emailSender = emailSender;
    }

    public async Task<int> ProcessBatchAsync(int maxItems, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var messages = await _dbContext.OutboxMessages
            .Where(x => (x.Status == OutboxStatus.Pending && x.NextAttemptAt <= now)
                || (x.Status == OutboxStatus.Processing && x.LockedUntil != null && x.LockedUntil < now))
            .OrderBy(x => x.CreatedAt)
            .Take(Math.Clamp(maxItems, 1, 50))
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            message.Status = OutboxStatus.Processing;
            message.LockedUntil = now.AddMinutes(2);
            message.Attempts++;
            message.UpdatedAt = now;
        }

        if (messages.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        foreach (var message in messages)
        {
            await DeliverAsync(message, cancellationToken);
        }

        return messages.Count;
    }

    private async Task DeliverAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        try
        {
            if (message.Type != "email.send")
            {
                throw new InvalidOperationException($"Unknown outbox message type '{message.Type}'.");
            }

            var payload = JsonSerializer.Deserialize<EmailNotificationPayload>(message.PayloadJson, JsonOptions)
                ?? throw new InvalidOperationException("The outbox payload could not be read.");

            await _emailSender.SendAsync(new EmailMessage(payload.To, payload.Subject, payload.HtmlBody, payload.TextBody), cancellationToken);

            message.Status = OutboxStatus.Completed;
            message.ProcessedAt = _clock.UtcNow;
            message.LockedUntil = null;
            message.LastError = null;
            message.UpdatedAt = _clock.UtcNow;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var now = _clock.UtcNow;
            message.Status = message.Attempts >= MaxAttempts ? OutboxStatus.Failed : OutboxStatus.Pending;
            message.NextAttemptAt = now.AddMinutes(Math.Pow(2, Math.Min(message.Attempts, 5)));
            message.LockedUntil = null;
            message.LastError = exception.Message.Length > 1000 ? exception.Message[..1000] : exception.Message;
            message.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
