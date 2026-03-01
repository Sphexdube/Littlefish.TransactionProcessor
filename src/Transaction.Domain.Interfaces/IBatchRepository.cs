using Transaction.Domain.Entities;

namespace Transaction.Domain.Interfaces;

public interface IBatchRepository : IRepository<Batch, Guid>
{
}
