using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transaction.Domain.Entities;

namespace Transaction.Infrastructure.Persistence.Mappings;

internal sealed class TransactionRecordMapping : IEntityTypeConfiguration<TransactionRecord>
{
    public void Configure(EntityTypeBuilder<TransactionRecord> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TransactionId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.MerchantId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Amount)
            .HasPrecision(18, 2);

        builder.Property(e => e.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(e => e.OriginalTransactionId)
            .HasMaxLength(100);

        builder.Property(e => e.RejectionReason)
            .HasMaxLength(500);

        builder.Property(e => e.Metadata)
            .HasColumnType("nvarchar(max)");

        builder.HasIndex(e => new { e.TenantId, e.TransactionId })
            .IsUnique();

        builder.HasIndex(e => new { e.TenantId, e.MerchantId, e.OccurredAt });

        builder.HasIndex(e => e.Status);

        builder.HasIndex(e => e.BatchId);

        builder.HasOne(e => e.Tenant)
            .WithMany(t => t.Transactions)
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Batch)
            .WithMany(b => b.Transactions)
            .HasForeignKey(e => e.BatchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
