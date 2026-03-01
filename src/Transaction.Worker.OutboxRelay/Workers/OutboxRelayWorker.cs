using Transaction.Domain.Entities;
using Transaction.Domain.Interfaces;
using Transaction.Domain.Observability;
using Transaction.Domain.Observability.Contracts;
using Transaction.Infrastructure.Messaging;

namespace Transaction.Worker.OutboxRelay.Workers;

public sealed class OutboxRelayWorker(
    IServiceScopeFactory scopeFactory,
    IObservabilityManager observabilityManager,
    IMetricRecorder metricRecorder,
    IConfiguration configuration) : BackgroundService
{
    private readonly int _batchSize = configuration.GetValue<int>("OutboxRelay:BatchSize", 100);
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(configuration.GetValue<int>("OutboxRelay:PollingIntervalSeconds", 1));
    private readonly string _queueName = configuration.GetValue<string>("OutboxRelay:QueueName") ?? "transactions-ingest";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        observabilityManager.LogMessage(InfoMessages.MethodStarted).AsInfo();

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
                metricRecorder.Increment(MetricDefinitions.OutboxRelayErrors);
                observabilityManager.LogMessage(string.Format(LogMessages.OutboxRelayUnexpectedError, ex.Message)).AsError();
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        observabilityManager.LogMessage(InfoMessages.MethodCompleted).AsInfo();
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();

        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        IServiceBusPublisher publisher = scope.ServiceProvider.GetRequiredService<IServiceBusPublisher>();

        IReadOnlyList<OutboxMessage> messages = await unitOfWork.OutboxMessages
            .GetUnpublishedAsync(_batchSize, cancellationToken);

        if (messages.Count == 0)
        {
            return;
        }

        observabilityManager.LogMessage(string.Format(LogMessages.OutboxRelayingMessages, messages.Count, _queueName)).AsInfo();

        foreach (OutboxMessage message in messages)
        {
            await publisher.PublishAsync(_queueName, message.Payload, cancellationToken);

            message.MarkAsPublished();

            unitOfWork.OutboxMessages.Update(message);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        metricRecorder.Increment(MetricDefinitions.OutboxMessagesRelayed, messages.Count);
        observabilityManager.LogMessage(string.Format(LogMessages.OutboxRelayedMessages, messages.Count)).AsInfo();
    }
}
