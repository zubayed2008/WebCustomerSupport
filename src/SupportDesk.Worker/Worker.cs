using SupportDesk.Application.Services;

namespace SupportDesk.Worker;

/// <summary>
/// Runs the outbox dispatcher, SLA monitor, and AI suggestion processor on
/// independent intervals. Each iteration uses its own scope so a failed batch
/// cannot poison the next one.
/// </summary>
public sealed class SupportDeskBackgroundWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SupportDeskBackgroundWorker> _logger;

    public SupportDeskBackgroundWorker(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<SupportDeskBackgroundWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollSeconds = Math.Clamp(_configuration.GetValue("Worker:PollSeconds", 10), 2, 300);
        var slaSeconds = Math.Clamp(_configuration.GetValue("Worker:SlaSeconds", 60), 10, 3600);
        var batchSize = Math.Clamp(_configuration.GetValue("Worker:BatchSize", 20), 1, 50);
        var nextSlaRun = DateTimeOffset.UtcNow;

        _logger.LogInformation("SupportDesk worker started. Poll={PollSeconds}s Sla={SlaSeconds}s Batch={BatchSize}.", pollSeconds, slaSeconds, batchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                var outbox = scope.ServiceProvider.GetRequiredService<OutboxProcessor>();
                var dispatched = await outbox.ProcessBatchAsync(batchSize, stoppingToken);
                if (dispatched > 0)
                {
                    _logger.LogInformation("Dispatched {Count} outbox message(s).", dispatched);
                }

                var ai = scope.ServiceProvider.GetRequiredService<AiSuggestionProcessor>();
                var processed = await ai.ProcessBatchAsync(batchSize, stoppingToken);
                if (processed > 0)
                {
                    _logger.LogInformation("Processed {Count} AI suggestion(s).", processed);
                }

                if (DateTimeOffset.UtcNow >= nextSlaRun)
                {
                    var monitor = scope.ServiceProvider.GetRequiredService<SlaMonitor>();
                    var breaches = await monitor.RunAsync(stoppingToken);
                    if (breaches > 0)
                    {
                        _logger.LogInformation("Recorded {Count} SLA breach(es).", breaches);
                    }

                    nextSlaRun = DateTimeOffset.UtcNow.AddSeconds(slaSeconds);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Worker iteration failed. It will retry on the next interval.");
            }

            await Task.Delay(TimeSpan.FromSeconds(pollSeconds), stoppingToken);
        }
    }
}

