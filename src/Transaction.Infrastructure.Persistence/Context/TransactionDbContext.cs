using Microsoft.EntityFrameworkCore;
using Transaction.Domain.Entities;
using Transaction.Domain.Entities.LookupTables;

namespace Transaction.Infrastructure.Persistence.Context;

public sealed class TransactionDbContext : DbContext
{
    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<TransactionRecord> Transactions => Set<TransactionRecord>();

    public DbSet<Batch> Batches => Set<Batch>();

    public DbSet<MerchantDailySummary> MerchantDailySummaries => Set<MerchantDailySummary>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<TransactionTypeValue> TransactionTypes => Set<TransactionTypeValue>();

    public DbSet<TransactionStatusValue> TransactionStatuses => Set<TransactionStatusValue>();

    public DbSet<BatchStatusValue> BatchStatuses => Set<BatchStatusValue>();

    public DbSet<RuleWorkflow> RuleWorkflows => Set<RuleWorkflow>();

    public DbSet<BusinessRule> BusinessRules => Set<BusinessRule>();

    public TransactionDbContext(DbContextOptions<TransactionDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TransactionDbContext).Assembly);
    }
}
