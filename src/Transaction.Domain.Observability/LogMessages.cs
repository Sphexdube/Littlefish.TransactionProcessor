namespace Transaction.Domain.Observability;

public static class LogMessages
{
    // Transaction lifecycle
    public const string TransactionProcessed = "Transaction {0} processed with status {1}";
    public const string TransactionAlreadyProcessed = "Transaction {0} already processed (Status={1}). Completing message (idempotent).";
    public const string TransactionNotFoundDeadLetter = "TransactionRecord not found for TenantId={0}, TransactionId={1}. Dead-lettering.";
    public const string TenantNotFoundDeadLetter = "Tenant {0} not found. Dead-lettering.";
    public const string FailedToDeserialiseMessage = "Failed to deserialise message {0}. Dead-lettering.";

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
