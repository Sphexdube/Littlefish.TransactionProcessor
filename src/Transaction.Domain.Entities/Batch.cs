using Transaction.Domain.Entities.Base;
using Transaction.Domain.Entities.Enums;

namespace Transaction.Domain.Entities;

public class Batch : Entity<Guid>
{
    public Guid TenantId { get; set; }

    public BatchStatus Status { get; set; } = BatchStatus.Received;

    public int TotalCount { get; set; }

    public int AcceptedCount { get; set; }

    public int RejectedCount { get; set; }

    public int QueuedCount { get; set; }

    public string CorrelationId { get; set; } = string.Empty;

    public DateTimeOffset? CompletedAt { get; set; }

    public virtual Tenant Tenant { get; set; } = null!;

    public virtual ICollection<TransactionRecord> Transactions { get; set; } = new List<TransactionRecord>();
}
