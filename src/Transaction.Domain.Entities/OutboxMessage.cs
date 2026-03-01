using Transaction.Domain.Entities.Base;

namespace Transaction.Domain.Entities;

public sealed class OutboxMessage : Entity<Guid>
{
    public Guid TenantId { get; private set; }

    public string TransactionId { get; private set; } = string.Empty;

    public string Payload { get; private set; } = string.Empty;

    public bool Published { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage Create(Guid id, Guid tenantId, string transactionId, string payload)
    {
        return new()
        {
            Id = id,
            TenantId = tenantId,
            TransactionId = transactionId,
            Payload = payload,
            Published = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void MarkAsPublished()
    {
        Published = true;
        PublishedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
