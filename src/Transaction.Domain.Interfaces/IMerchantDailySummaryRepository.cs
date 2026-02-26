using Transaction.Domain.Entities;

namespace Transaction.Domain.Interfaces;

public interface IMerchantDailySummaryRepository : IRepository<MerchantDailySummary, Guid>
{
    Task<MerchantDailySummary?> GetByMerchantAndDateAsync(Guid tenantId, string merchantId, DateOnly date, CancellationToken cancellationToken = default);
}
