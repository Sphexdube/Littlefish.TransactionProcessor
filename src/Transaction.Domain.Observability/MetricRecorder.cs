using System.Diagnostics.Metrics;
using Transaction.Domain.Observability.Contracts;

namespace Transaction.Domain.Observability;

public sealed class MetricRecorder : IMetricRecorder, IDisposable
{
    public const string MeterName = "Littlefish.TransactionProcessor";

    private readonly Meter _meter;
    private readonly Dictionary<string, Counter<long>> _counters;
    private readonly Dictionary<string, Histogram<long>> _histograms;

    public MetricRecorder()
    {
        _meter = new Meter(MeterName);

        _counters = new Dictionary<string, Counter<long>>
        {
            [MetricDefinitions.TransactionsIngested] = _meter.CreateCounter<long>(MetricDefinitions.TransactionsIngested, description: "Number of transactions accepted into a batch"),
            [MetricDefinitions.TransactionsProcessed] = _meter.CreateCounter<long>(MetricDefinitions.TransactionsProcessed, description: "Number of transactions successfully processed"),
            [MetricDefinitions.TransactionsRejected] = _meter.CreateCounter<long>(MetricDefinitions.TransactionsRejected, description: "Number of transactions rejected by business rules or validation"),
            [MetricDefinitions.TransactionsInReview] = _meter.CreateCounter<long>(MetricDefinitions.TransactionsInReview, description: "Number of transactions flagged for manual review"),
            [MetricDefinitions.BatchesReceived] = _meter.CreateCounter<long>(MetricDefinitions.BatchesReceived, description: "Number of transaction batches received"),
            [MetricDefinitions.BatchesCompleted] = _meter.CreateCounter<long>(MetricDefinitions.BatchesCompleted, description: "Number of transaction batches fully processed"),
            [MetricDefinitions.DailyLimitChecks] = _meter.CreateCounter<long>(MetricDefinitions.DailyLimitChecks, description: "Number of daily merchant limit checks performed"),
            [MetricDefinitions.DailyLimitExceeded] = _meter.CreateCounter<long>(MetricDefinitions.DailyLimitExceeded, description: "Number of transactions rejected due to daily merchant limit"),
            [MetricDefinitions.OutboxMessagesRelayed] = _meter.CreateCounter<long>(MetricDefinitions.OutboxMessagesRelayed, description: "Number of outbox messages successfully relayed to Service Bus"),
            [MetricDefinitions.OutboxRelayErrors] = _meter.CreateCounter<long>(MetricDefinitions.OutboxRelayErrors, description: "Number of unexpected errors during outbox relay"),
            [MetricDefinitions.ServiceBusDeadLettered] = _meter.CreateCounter<long>(MetricDefinitions.ServiceBusDeadLettered, description: "Number of messages sent to the dead-letter queue"),
            [MetricDefinitions.ServiceBusAbandoned] = _meter.CreateCounter<long>(MetricDefinitions.ServiceBusAbandoned, description: "Number of messages abandoned for redelivery"),
            [MetricDefinitions.ConcurrencyConflicts] = _meter.CreateCounter<long>(MetricDefinitions.ConcurrencyConflicts, description: "Number of optimistic concurrency conflicts encountered"),
        };

        _histograms = new Dictionary<string, Histogram<long>>
        {
            [MetricDefinitions.ProcessingDuration] = _meter.CreateHistogram<long>(MetricDefinitions.ProcessingDuration, unit: "ms", description: "Time taken to process a transaction from receipt to acknowledgement"),
        };
    }

    public void Increment(string name, long value = 1)
    {
        if (_counters.TryGetValue(name, out Counter<long>? counter))
            counter.Add(value);
    }

    public void RecordDuration(string name, long milliseconds)
    {
        if (_histograms.TryGetValue(name, out Histogram<long>? histogram))
            histogram.Record(milliseconds);
    }

    public void Dispose() => _meter.Dispose();
}
