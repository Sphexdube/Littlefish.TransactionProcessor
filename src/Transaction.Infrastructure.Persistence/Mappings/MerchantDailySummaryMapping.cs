using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transaction.Domain.Entities;

namespace Transaction.Infrastructure.Persistence.Mappings;

internal sealed class MerchantDailySummaryMapping : IEntityTypeConfiguration<MerchantDailySummary>
{
    public void Configure(EntityTypeBuilder<MerchantDailySummary> builder)
    {
        builder.ToTable("MerchantDailySummaries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.MerchantId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.Version)
            .IsRowVersion();

        builder.HasIndex(e => new { e.TenantId, e.MerchantId, e.Date })
            .IsUnique();

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
