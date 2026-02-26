using Transaction.Domain.Entities;

namespace Transaction.Domain.Interfaces;

public interface IBatchRepository : IRepository<Batch, Guid>
{
    Task<IEnumerable<Batch>> GetPendingBatchesAsync(int limit, CancellationToken cancellationToken = default);
}
