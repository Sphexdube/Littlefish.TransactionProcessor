using Transaction.Domain.Entities.Base;

namespace Transaction.Domain.Entities;

public sealed class MerchantDailySummary : Entity<Guid>
{
    public Guid TenantId { get; private set; }

    public string MerchantId { get; private set; } = string.Empty;

    public DateOnly Date { get; private set; }

    public decimal TotalAmount { get; private set; }

    public int TransactionCount { get; private set; }

    public DateTimeOffset LastCalculatedAt { get; private set; }

    public byte[] Version { get; private set; } = [];

    public Tenant Tenant { get; set; } = null!;

    private MerchantDailySummary() { }

    public static MerchantDailySummary Create(Guid tenantId, string merchantId, DateOnly date, decimal initialAmount)
    {
        return new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            MerchantId = merchantId,
            Date = date,
            TotalAmount = initialAmount,
            TransactionCount = 1,
            LastCalculatedAt = DateTimeOffset.UtcNow
        };
    }

    public void AddAmount(decimal amount)
    {
        TotalAmount += amount;
        TransactionCount++;
        LastCalculatedAt = DateTimeOffset.UtcNow;
    }
}
