using Transaction.Domain.Entities;
using Transaction.Domain.Interfaces;
using Transaction.Infrastructure.Persistence.Context;

namespace Transaction.Infrastructure.Persistence.Repositories;

public class BatchRepository : Repository<Batch, Guid>, IBatchRepository
{
    public BatchRepository(TransactionDbContext context)
        : base(context)
    {
    }
}
