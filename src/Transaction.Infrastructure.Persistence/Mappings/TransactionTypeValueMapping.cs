using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transaction.Domain.Entities.LookupTables;

namespace Transaction.Infrastructure.Persistence.Mappings;

internal sealed class TransactionTypeValueMapping : IEntityTypeConfiguration<TransactionTypeValue>
{
    public void Configure(EntityTypeBuilder<TransactionTypeValue> builder)
    {
        builder.ToTable("TransactionTypes");

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
