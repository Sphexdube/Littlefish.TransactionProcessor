namespace Transaction.Application.Models.Response.V1;

public sealed record IngestBatchResponse
{
    public required Guid BatchId { get; init; }

    public required int AcceptedCount { get; init; }

    public required int RejectedCount { get; init; }

    public required int QueuedCount { get; init; }

    public required string CorrelationId { get; init; }

    public IList<TransactionValidationError>? Errors { get; init; }
}
