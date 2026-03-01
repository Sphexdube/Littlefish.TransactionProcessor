using Transaction.Domain.Entities;
using Transaction.Domain.Interfaces;
using Transaction.Infrastructure.Persistence.Context;

namespace Transaction.Infrastructure.Persistence.Repositories;

public class BatchRepository(TransactionDbContext context)
    : Repository<Batch, Guid>(context), IBatchRepository
{
}
