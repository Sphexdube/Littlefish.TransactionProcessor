using Microsoft.EntityFrameworkCore;
using Transaction.Domain.Entities;
using Transaction.Domain.Interfaces;
using Transaction.Infrastructure.Persistence.Context;

namespace Transaction.Infrastructure.Persistence.Repositories;

public class MerchantDailySummaryRepository : Repository<MerchantDailySummary, Guid>, IMerchantDailySummaryRepository
{
    public MerchantDailySummaryRepository(TransactionDbContext context)
        : base(context)
    {
    }

    public async Task<MerchantDailySummary?> GetByMerchantAndDateAsync(
        Guid tenantId,
        string merchantId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(
            s => s.TenantId == tenantId && s.MerchantId == merchantId && s.Date == date,
            cancellationToken);
    }
}
