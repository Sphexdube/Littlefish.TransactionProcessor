using Transaction.Domain.Commands;

namespace Transaction.Application.Models.Request.V1;

public sealed record IngestTransactionBatchRequest
{
    public required IList<TransactionItemRequest> Transactions { get; init; }

    public IngestBatchCommand BuildCommand(Guid tenantId, string correlationId)
    {
        return new IngestBatchCommand
        {
            TenantId = tenantId,
            CorrelationId = correlationId,
            Transactions = Transactions
                .Select(t => new TransactionItemCommand
                {
                    TransactionId = t.TransactionId,
                    OccurredAt = t.OccurredAt,
                    Amount = t.Amount,
                    Currency = t.Currency,
                    MerchantId = t.MerchantId,
                    Type = t.Type,
                    OriginalTransactionId = t.OriginalTransactionId,
                    Metadata = t.Metadata
                })
                .ToList()
        };
    }
}
