namespace SupportDesk.Application.Abstractions;

public interface IOutboxWriter
{
    Task EnqueueAsync(string type, object payload, string idempotencyKey, CancellationToken cancellationToken);
}
