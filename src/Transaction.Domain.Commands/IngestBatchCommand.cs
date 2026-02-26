namespace Transaction.Domain.Commands;

public sealed record IngestBatchCommand
{
    public required Guid TenantId { get; init; }

    public required IReadOnlyList<TransactionItemCommand> Transactions { get; init; }

    public required string CorrelationId { get; init; }
}
