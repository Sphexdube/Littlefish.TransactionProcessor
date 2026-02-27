namespace Transaction.Domain.Observability;

public static class MetricDefinitions
{
    // Transaction lifecycle
    public const string TransactionsIngested = "transactions.ingested";
    public const string TransactionsProcessed = "transactions.processed";
    public const string TransactionsRejected = "transactions.rejected";
    public const string TransactionsInReview = "transactions.in_review";

    // Batch lifecycle
    public const string BatchesReceived = "batches.received";
    public const string BatchesCompleted = "batches.completed";

    // Processing performance
    public const string ProcessingDuration = "processing.duration_ms";

    // Business rules
    public const string DailyLimitChecks = "daily_limit.checks";
    public const string DailyLimitExceeded = "daily_limit.exceeded";

    // Outbox relay
    public const string OutboxMessagesRelayed = "outbox.messages_relayed";
    public const string OutboxRelayErrors = "outbox.relay_errors";

    // Service Bus
    public const string ServiceBusDeadLettered = "servicebus.dead_lettered";
    public const string ServiceBusAbandoned = "servicebus.abandoned";
    public const string ConcurrencyConflicts = "transactions.concurrency_conflicts";
}
