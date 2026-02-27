using Transaction.Domain.Entities;
using Transaction.Domain.Interfaces;
using Transaction.Domain.Observability;
using Transaction.Domain.Observability.Contracts;
using Transaction.Infrastructure.Messaging;

namespace Transaction.Worker.OutboxRelay.Workers;

public sealed class OutboxRelayWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IObservabilityManager _observabilityManager;
    private readonly IMetricRecorder _metricRecorder;
    private readonly int _batchSize;
    private readonly TimeSpan _pollingInterval;
    private readonly string _queueName;

    public OutboxRelayWorker(
        IServiceScopeFactory scopeFactory,
        IObservabilityManager observabilityManager,
        IMetricRecorder metricRecorder,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _observabilityManager = observabilityManager;
        _metricRecorder = metricRecorder;
        _batchSize = configuration.GetValue<int>("OutboxRelay:BatchSize", 100);
        _pollingInterval = TimeSpan.FromSeconds(configuration.GetValue<int>("OutboxRelay:PollingIntervalSeconds", 1));
        _queueName = configuration.GetValue<string>("OutboxRelay:QueueName") ?? "transactions-ingest";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _observabilityManager.LogMessage(InfoMessages.MethodStarted).AsInfo();

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
                _metricRecorder.Increment(MetricDefinitions.OutboxRelayErrors);
                _observabilityManager.LogMessage(string.Format(LogMessages.OutboxRelayUnexpectedError, ex.Message)).AsError();
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _observabilityManager.LogMessage(InfoMessages.MethodCompleted).AsInfo();
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

        _observabilityManager.LogMessage(string.Format(LogMessages.OutboxRelayingMessages, messages.Count, _queueName)).AsInfo();

        foreach (OutboxMessage message in messages)
        {
            await publisher.PublishAsync(_queueName, message.Payload, cancellationToken);

            message.MarkAsPublished();

            unitOfWork.OutboxMessages.Update(message);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        _metricRecorder.Increment(MetricDefinitions.OutboxMessagesRelayed, messages.Count);
        _observabilityManager.LogMessage(string.Format(LogMessages.OutboxRelayedMessages, messages.Count)).AsInfo();
    }
}
