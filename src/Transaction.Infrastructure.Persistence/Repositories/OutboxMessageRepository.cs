using Microsoft.EntityFrameworkCore;
using Transaction.Domain.Entities;
using Transaction.Domain.Interfaces;
using Transaction.Infrastructure.Persistence.Context;

namespace Transaction.Infrastructure.Persistence.Repositories;

public sealed class OutboxMessageRepository : Repository<OutboxMessage, Guid>, IOutboxMessageRepository
{
    public OutboxMessageRepository(TransactionDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetUnpublishedAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(m => !m.Published)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }
}
