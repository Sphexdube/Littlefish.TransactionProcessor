using Microsoft.EntityFrameworkCore.Storage;
using Transaction.Domain.Interfaces;
using Transaction.Infrastructure.Persistence.Context;
using Transaction.Infrastructure.Persistence.Repositories;

namespace Transaction.Infrastructure.Persistence;

public class UnitOfWork(TransactionDbContext context) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;

    private ITransactionRepository? _transactions;
    private IBatchRepository? _batches;
    private ITenantRepository? _tenants;
    private IMerchantDailySummaryRepository? _merchantDailySummaries;
    private IOutboxMessageRepository? _outboxMessages;

    public ITransactionRepository Transactions =>
        _transactions ??= new TransactionRepository(context);

    public IBatchRepository Batches =>
        _batches ??= new BatchRepository(context);

    public ITenantRepository Tenants =>
        _tenants ??= new TenantRepository(context);

    public IMerchantDailySummaryRepository MerchantDailySummaries =>
        _merchantDailySummaries ??= new MerchantDailySummaryRepository(context);

    public IOutboxMessageRepository OutboxMessages =>
        _outboxMessages ??= new OutboxMessageRepository(context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        context.Dispose();
    }
}
