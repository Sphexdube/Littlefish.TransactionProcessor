using Transaction.Domain.Entities.Base;

namespace Transaction.Domain.Entities;

public class Tenant : Entity<Guid>
{
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public decimal DailyMerchantLimit { get; set; } = 100000m;

    public decimal HighValueThreshold { get; set; } = 10000m;

    public virtual ICollection<TransactionRecord> Transactions { get; set; } = new List<TransactionRecord>();

    public virtual ICollection<Batch> Batches { get; set; } = new List<Batch>();
}
