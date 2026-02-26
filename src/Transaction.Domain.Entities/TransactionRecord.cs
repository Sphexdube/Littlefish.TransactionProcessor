using Transaction.Domain.Entities.Base;
using Transaction.Domain.Entities.Enums;

namespace Transaction.Domain.Entities;

public class TransactionRecord : Entity<Guid>
{
    public Guid TenantId { get; set; }

    public string TransactionId { get; set; } = string.Empty;

    public string MerchantId { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "USD";

    public TransactionType Type { get; set; }

    public string? OriginalTransactionId { get; set; }

    public TransactionStatus Status { get; set; } = TransactionStatus.Received;

    public DateTimeOffset OccurredAt { get; set; }

    public string? Metadata { get; set; }

    public Guid BatchId { get; set; }

    public string? RejectionReason { get; set; }

    public DateTimeOffset? ProcessedAt { get; set; }

    public virtual Tenant Tenant { get; set; } = null!;

    public virtual Batch Batch { get; set; } = null!;
}
