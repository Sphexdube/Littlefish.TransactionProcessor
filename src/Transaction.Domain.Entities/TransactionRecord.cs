using Transaction.Domain.Entities.Base;
using Transaction.Domain.Entities.Enums;

namespace Transaction.Domain.Entities;

public sealed class TransactionRecord : Entity<Guid>
{
    public Guid TenantId { get; private set; }

    public string TransactionId { get; private set; } = string.Empty;

    public string MerchantId { get; private set; } = string.Empty;

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = "USD";

    public TransactionType Type { get; private set; }

    public string? OriginalTransactionId { get; private set; }

    public TransactionStatus Status { get; private set; } = TransactionStatus.Received;

    public DateTimeOffset OccurredAt { get; private set; }

    public string? Metadata { get; private set; }

    public Guid BatchId { get; private set; }

    public string? RejectionReason { get; private set; }

    public DateTimeOffset? ProcessedAt { get; private set; }

    public Tenant Tenant { get; set; } = null!;

    public Batch Batch { get; set; } = null!;

    private TransactionRecord() { }

    public static TransactionRecord Create(
        Guid tenantId,
        Guid batchId,
        string transactionId,
        string merchantId,
        decimal amount,
        string currency,
        TransactionType type,
        string? originalTransactionId,
        DateTimeOffset occurredAt,
        string? metadata)
    {
        return new TransactionRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            BatchId = batchId,
            TransactionId = transactionId,
            MerchantId = merchantId,
            Amount = amount,
            Currency = currency,
            Type = type,
            OriginalTransactionId = originalTransactionId,
            OccurredAt = occurredAt,
            Status = TransactionStatus.Received,
            Metadata = metadata
        };
    }

    public void BeginProcessing()
    {
        Status = TransactionStatus.Processing;
    }

    public void Complete()
    {
        Status = TransactionStatus.Processed;
        ProcessedAt = DateTimeOffset.UtcNow;
    }

    public void Reject(string reason)
    {
        Status = TransactionStatus.Rejected;
        RejectionReason = reason;
        ProcessedAt = DateTimeOffset.UtcNow;
    }

    public void MarkForReview(string? reason)
    {
        Status = TransactionStatus.Review;
        RejectionReason = reason;
        ProcessedAt = DateTimeOffset.UtcNow;
    }
}
