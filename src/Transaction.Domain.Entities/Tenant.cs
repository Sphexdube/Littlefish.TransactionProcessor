using Transaction.Domain.Entities.Base;

namespace Transaction.Domain.Entities;

public sealed class Tenant : Entity<Guid>
{
    public string Name { get; private set; } = string.Empty;

    public bool IsActive { get; private set; } = true;

    public decimal DailyMerchantLimit { get; private set; } = 100000m;

    public decimal HighValueThreshold { get; private set; } = 10000m;

    public ICollection<TransactionRecord> Transactions { get; set; } = new List<TransactionRecord>();

    public ICollection<Batch> Batches { get; set; } = new List<Batch>();
}
