using Transaction.Domain.Entities;

namespace Transaction.Domain.Interfaces;

public interface IOutboxMessageRepository : IRepository<OutboxMessage, Guid>
{
    Task<IReadOnlyList<OutboxMessage>> GetUnpublishedAsync(int batchSize, CancellationToken cancellationToken = default);
}
