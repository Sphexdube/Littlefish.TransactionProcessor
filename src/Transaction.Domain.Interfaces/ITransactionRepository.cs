using Transaction.Domain.Entities;

namespace Transaction.Domain.Interfaces;

public interface ITransactionRepository : IRepository<TransactionRecord, Guid>
{
    Task<TransactionRecord?> GetByTransactionIdAsync(Guid tenantId, string transactionId, CancellationToken cancellationToken = default);

    Task<bool> ExistsByTransactionIdAsync(Guid tenantId, string transactionId, CancellationToken cancellationToken = default);

}
