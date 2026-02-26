namespace Transaction.Application.Models.Response.V1;

public record IngestBatchResponse(
    Guid BatchId,
    int AcceptedCount,
    int RejectedCount,
    int QueuedCount,
    string CorrelationId,
    IList<TransactionValidationError>? Errors);
