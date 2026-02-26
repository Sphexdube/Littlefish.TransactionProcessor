namespace Transaction.Domain.Observability;

public static class MetricDefinitions
{
    public const string TransactionsIngested = "transactions.ingested";

    public const string TransactionsProcessed = "transactions.processed";

    public const string TransactionsRejected = "transactions.rejected";

    public const string TransactionsInReview = "transactions.in_review";

    public const string BatchesReceived = "batches.received";

    public const string BatchesCompleted = "batches.completed";

    public const string ProcessingDuration = "processing.duration_ms";

    public const string DailyLimitChecks = "daily_limit.checks";

    public const string DailyLimitExceeded = "daily_limit.exceeded";
}
