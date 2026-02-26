using Transaction.Domain.Entities;
using Transaction.Domain.Interfaces;
using Transaction.Infrastructure.Messaging;

namespace Transaction.Worker.OutboxRelay.Workers;

public sealed class OutboxRelayWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxRelayWorker> _logger;
    private readonly int _batchSize;
    private readonly TimeSpan _pollingInterval;
    private readonly string _queueName;

    public OutboxRelayWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxRelayWorker> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _batchSize = configuration.GetValue<int>("OutboxRelay:BatchSize", 100);
        _pollingInterval = TimeSpan.FromSeconds(configuration.GetValue<int>("OutboxRelay:PollingIntervalSeconds", 1));
        _queueName = configuration.GetValue<string>("OutboxRelay:QueueName") ?? "transactions-ingest";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxRelayWorker started. Polling every {Interval}s, batch size {BatchSize}.",
            _pollingInterval.TotalSeconds, _batchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in OutboxRelayWorker.");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("OutboxRelayWorker stopping.");
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();

        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        IServiceBusPublisher publisher = scope.ServiceProvider.GetRequiredService<IServiceBusPublisher>();

        IReadOnlyList<OutboxMessage> messages = await unitOfWork.OutboxMessages
            .GetUnpublishedAsync(_batchSize, cancellationToken);

        if (messages.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Relaying {Count} outbox message(s) to queue '{Queue}'.",
            messages.Count, _queueName);

        foreach (OutboxMessage message in messages)
        {
            await publisher.PublishAsync(_queueName, message.Payload, cancellationToken);

            message.MarkAsPublished();

            unitOfWork.OutboxMessages.Update(message);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Relayed {Count} message(s) successfully.", messages.Count);
    }
}
