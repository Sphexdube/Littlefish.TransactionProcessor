using Transaction.Domain.Entities;

namespace Transaction.Domain.Interfaces;

public interface ITransactionRepository : IRepository<TransactionRecord, Guid>
{
    Task<TransactionRecord?> GetByTransactionIdAsync(Guid tenantId, string transactionId, CancellationToken cancellationToken = default);

    Task<bool> ExistsByTransactionIdAsync(Guid tenantId, string transactionId, CancellationToken cancellationToken = default);

    Task<IEnumerable<TransactionRecord>> GetByBatchIdAsync(Guid batchId, CancellationToken cancellationToken = default);

    Task<decimal> GetDailyMerchantTotalAsync(Guid tenantId, string merchantId, DateOnly date, CancellationToken cancellationToken = default);

    Task<IEnumerable<TransactionRecord>> GetPendingTransactionsAsync(int batchSize, CancellationToken cancellationToken = default);

    Task<TransactionRecord?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
}
