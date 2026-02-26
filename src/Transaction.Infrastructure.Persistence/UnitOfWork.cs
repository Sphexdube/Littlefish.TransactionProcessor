using Microsoft.EntityFrameworkCore.Storage;
using Transaction.Domain.Interfaces;
using Transaction.Infrastructure.Persistence.Context;
using Transaction.Infrastructure.Persistence.Repositories;

namespace Transaction.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly TransactionDbContext _context;
    private IDbContextTransaction? _transaction;

    private ITransactionRepository? _transactions;
    private IBatchRepository? _batches;
    private ITenantRepository? _tenants;
    private IMerchantDailySummaryRepository? _merchantDailySummaries;
    private IOutboxMessageRepository? _outboxMessages;

    public ITransactionRepository Transactions =>
        _transactions ??= new TransactionRepository(_context);

    public IBatchRepository Batches =>
        _batches ??= new BatchRepository(_context);

    public ITenantRepository Tenants =>
        _tenants ??= new TenantRepository(_context);

    public IMerchantDailySummaryRepository MerchantDailySummaries =>
        _merchantDailySummaries ??= new MerchantDailySummaryRepository(_context);

    public IOutboxMessageRepository OutboxMessages =>
        _outboxMessages ??= new OutboxMessageRepository(_context);

    public UnitOfWork(TransactionDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
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
        _context.Dispose();
    }
}
