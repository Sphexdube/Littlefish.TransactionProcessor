using Microsoft.EntityFrameworkCore;
using Transaction.Domain.Entities;
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
}
