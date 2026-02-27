namespace Transaction.Domain.Observability;

public static class LogMessages
{
    // Batch lifecycle
    public const string BatchReceived = "Batch {0} received with {1} transactions for tenant {2}";
    public const string BatchProcessingStarted = "Processing batch {0} for tenant {1}";
    public const string BatchProcessingCompleted = "Batch {0} processing completed. Accepted: {1}, Rejected: {2}";

    // Transaction lifecycle
    public const string TransactionProcessed = "Transaction {0} processed with status {1}";
    public const string TransactionRejected = "Transaction {0} rejected: {1}";
    public const string TransactionAlreadyProcessed = "Transaction {0} already processed (Status={1}). Completing message (idempotent).";
    public const string TransactionNotFoundDeadLetter = "TransactionRecord not found for TenantId={0}, TransactionId={1}. Dead-lettering.";
    public const string TenantNotFoundDeadLetter = "Tenant {0} not found. Dead-lettering.";
    public const string FailedToDeserialiseMessage = "Failed to deserialise message {0}. Dead-lettering.";

    // Business rules
    public const string DailyLimitExceeded = "Daily limit exceeded for merchant {0}. Limit: {1}, Current: {2}";
    public const string HighValueTransaction = "High value transaction {0} marked for review. Amount: {1}";

    // Concurrency
    public const string ConcurrencyConflictRetry = "Concurrency conflict processing {0}, retrying (attempt {1}/{2}).";
    public const string ConcurrencyConflictExhausted = "Concurrency conflict on {0} exhausted retries. Abandoning for redelivery.";

    // Outbox relay
    public const string OutboxRelayingMessages = "Relaying {0} outbox message(s) to queue '{1}'.";
    public const string OutboxRelayedMessages = "Relayed {0} message(s) successfully.";
    public const string OutboxRelayUnexpectedError = "Unexpected error in OutboxRelayWorker: {0}";

    // Service Bus
    public const string ServiceBusError = "Service Bus error. Source={0}, Entity={1}: {2}";
    public const string UnexpectedErrorProcessingTransaction = "Unexpected error processing transaction {0}: {1}";
}
