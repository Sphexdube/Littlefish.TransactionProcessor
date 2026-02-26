using Microsoft.EntityFrameworkCore;
using Transaction.Domain.Entities;
using Transaction.Domain.Entities.Enums;
using Transaction.Domain.Interfaces;
using Transaction.Infrastructure.Persistence.Context;

namespace Transaction.Infrastructure.Persistence.Repositories;

public class BatchRepository : Repository<Batch, Guid>, IBatchRepository
{
    public BatchRepository(TransactionDbContext context)
        : base(context)
    {
    }

    public async Task<IEnumerable<Batch>> GetPendingBatchesAsync(int limit, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(b => b.Status == BatchStatus.Received || b.Status == BatchStatus.Processing)
            .OrderBy(b => b.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
