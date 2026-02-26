namespace Transaction.Domain.Commands;

public sealed record GetTransactionQuery
{
    public required Guid TenantId { get; init; }

    public required string TransactionId { get; init; }
}
