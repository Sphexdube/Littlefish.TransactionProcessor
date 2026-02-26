namespace Transaction.Domain.Observability;

public static class LogMessages
{
    public const string BatchReceived = "Batch {BatchId} received with {TransactionCount} transactions for tenant {TenantId}";

    public const string BatchProcessingStarted = "Processing batch {BatchId} for tenant {TenantId}";

    public const string BatchProcessingCompleted = "Batch {BatchId} processing completed. Accepted: {AcceptedCount}, Rejected: {RejectedCount}";

    public const string TransactionProcessed = "Transaction {TransactionId} processed with status {Status}";

    public const string TransactionRejected = "Transaction {TransactionId} rejected: {Reason}";

    public const string DailyLimitExceeded = "Daily limit exceeded for merchant {MerchantId}. Limit: {Limit}, Current: {Current}";

    public const string HighValueTransaction = "High value transaction {TransactionId} marked for review. Amount: {Amount}";
}
