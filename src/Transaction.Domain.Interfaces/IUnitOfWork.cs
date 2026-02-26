namespace Transaction.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    ITransactionRepository Transactions { get; }

    IBatchRepository Batches { get; }

    ITenantRepository Tenants { get; }

    IMerchantDailySummaryRepository MerchantDailySummaries { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
