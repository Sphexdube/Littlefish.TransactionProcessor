using Microsoft.EntityFrameworkCore;
using Transaction.Domain.Entities;
using Transaction.Domain.Entities.Enums;

namespace Transaction.Infrastructure.Persistence.Context;

public class TransactionDbContext : DbContext
{
    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<TransactionRecord> Transactions => Set<TransactionRecord>();

    public DbSet<Batch> Batches => Set<Batch>();

    public DbSet<MerchantDailySummary> MerchantDailySummaries => Set<MerchantDailySummary>();

    public TransactionDbContext(DbContextOptions<TransactionDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("Tenants");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.DailyMerchantLimit).HasPrecision(18, 2);
            entity.Property(e => e.HighValueThreshold).HasPrecision(18, 2);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<TransactionRecord>(entity =>
        {
            entity.ToTable("Transactions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TransactionId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.MerchantId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Currency).HasMaxLength(3).IsRequired();
            entity.Property(e => e.OriginalTransactionId).HasMaxLength(100);
            entity.Property(e => e.RejectionReason).HasMaxLength(500);
            entity.Property(e => e.Metadata).HasColumnType("nvarchar(max)");

            entity.HasIndex(e => new { e.TenantId, e.TransactionId }).IsUnique();
            entity.HasIndex(e => new { e.TenantId, e.MerchantId, e.OccurredAt });
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.BatchId);

            entity.HasOne(e => e.Tenant)
                  .WithMany(t => t.Transactions)
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Batch)
                  .WithMany(b => b.Transactions)
                  .HasForeignKey(e => e.BatchId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Batch>(entity =>
        {
            entity.ToTable("Batches");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CorrelationId).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.Tenant)
                  .WithMany(t => t.Batches)
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MerchantDailySummary>(entity =>
        {
            entity.ToTable("MerchantDailySummaries");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MerchantId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);

            entity.HasIndex(e => new { e.TenantId, e.MerchantId, e.Date }).IsUnique();

            entity.HasOne(e => e.Tenant)
                  .WithMany()
                  .HasForeignKey(e => e.TenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
