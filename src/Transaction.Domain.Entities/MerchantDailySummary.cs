using Transaction.Domain.Entities.Base;

namespace Transaction.Domain.Entities;

public class MerchantDailySummary : Entity<Guid>
{
    public Guid TenantId { get; set; }

    public string MerchantId { get; set; } = string.Empty;

    public DateOnly Date { get; set; }

    public decimal TotalAmount { get; set; }

    public int TransactionCount { get; set; }

    public DateTimeOffset LastCalculatedAt { get; set; }

    public byte[] Version { get; set; } = [];

    public virtual Tenant Tenant { get; set; } = null!;
}
