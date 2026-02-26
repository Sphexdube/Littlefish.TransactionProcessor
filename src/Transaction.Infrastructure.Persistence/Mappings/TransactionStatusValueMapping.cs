using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transaction.Domain.Entities.LookupTables;

namespace Transaction.Infrastructure.Persistence.Mappings;

internal sealed class TransactionStatusValueMapping : IEntityTypeConfiguration<TransactionStatusValue>
{
    public void Configure(EntityTypeBuilder<TransactionStatusValue> builder)
    {
        builder.ToTable("TransactionStatuses");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.Name)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(200);

        builder.HasIndex(e => e.Name)
            .IsUnique();
    }
}
