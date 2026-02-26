using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transaction.Domain.Entities;

namespace Transaction.Infrastructure.Persistence.Mappings;

internal sealed class BatchMapping : IEntityTypeConfiguration<Batch>
{
    public void Configure(EntityTypeBuilder<Batch> builder)
    {
        builder.ToTable("Batches");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.CorrelationId)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(e => e.TenantId);

        builder.HasIndex(e => e.Status);

        builder.HasOne(e => e.Tenant)
            .WithMany(t => t.Batches)
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
