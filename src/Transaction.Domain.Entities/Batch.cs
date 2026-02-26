using Transaction.Domain.Entities.Base;
using Transaction.Domain.Entities.Enums;

namespace Transaction.Domain.Entities;

public sealed class Batch : Entity<Guid>
{
    public Guid TenantId { get; private set; }

    public BatchStatus Status { get; private set; } = BatchStatus.Received;

    public int TotalCount { get; private set; }

    public int AcceptedCount { get; private set; }

    public int RejectedCount { get; private set; }

    public int QueuedCount { get; private set; }

    public string CorrelationId { get; private set; } = string.Empty;

    public DateTimeOffset? CompletedAt { get; private set; }

    public Tenant Tenant { get; set; } = null!;

    public ICollection<TransactionRecord> Transactions { get; set; } = new List<TransactionRecord>();

    private Batch() { }

    public static Batch Create(Guid tenantId, int totalCount, string correlationId)
    {
        return new Batch
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Status = BatchStatus.Received,
            TotalCount = totalCount,
            CorrelationId = correlationId
        };
    }

    public void UpdateCounts(int accepted, int rejected, int queued)
    {
        AcceptedCount = accepted;
        RejectedCount = rejected;
        QueuedCount = queued;
        Status = BatchStatus.Processing;
    }
}
