using Microsoft.EntityFrameworkCore;
using Transaction.Domain.Entities;
using Transaction.Domain.Entities.Enums;
using Transaction.Domain.Interfaces;
using Transaction.Infrastructure.Persistence.Context;

namespace Transaction.Infrastructure.Persistence.Repositories;

public class TransactionRepository : Repository<TransactionRecord, Guid>, ITransactionRepository
{
    public TransactionRepository(TransactionDbContext context)
        : base(context)
    {
    }

    public async Task<TransactionRecord?> GetByTransactionIdAsync(
        Guid tenantId,
        string transactionId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(
            t => t.TenantId == tenantId && t.TransactionId == transactionId,
            cancellationToken);
    }

    public async Task<bool> ExistsByTransactionIdAsync(
        Guid tenantId,
        string transactionId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(
            t => t.TenantId == tenantId && t.TransactionId == transactionId,
            cancellationToken);
    }

    public async Task<IEnumerable<TransactionRecord>> GetByBatchIdAsync(
        Guid batchId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(t => t.BatchId == batchId)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetDailyMerchantTotalAsync(
        Guid tenantId,
        string merchantId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var startOfDay = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endOfDay = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        return await DbSet
            .Where(t =>
                t.TenantId == tenantId &&
                t.MerchantId == merchantId &&
                t.Type == TransactionType.Purchase &&
                t.Status == TransactionStatus.Processed &&
                t.OccurredAt >= (DateTimeOffset)startOfDay &&
                t.OccurredAt <= (DateTimeOffset)endOfDay)
            .SumAsync(t => t.Amount, cancellationToken);
    }

    public async Task<IEnumerable<TransactionRecord>> GetPendingTransactionsAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(t => t.Status == TransactionStatus.Received)
            .OrderBy(t => t.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<TransactionRecord?> GetByIdWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(t => t.Tenant)
            .Include(t => t.Batch)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }
}
