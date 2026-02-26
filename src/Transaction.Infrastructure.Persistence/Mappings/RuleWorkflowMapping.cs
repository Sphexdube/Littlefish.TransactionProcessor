using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transaction.Domain.Entities;

namespace Transaction.Infrastructure.Persistence.Mappings;

internal sealed class RuleWorkflowMapping : IEntityTypeConfiguration<RuleWorkflow>
{
    public void Configure(EntityTypeBuilder<RuleWorkflow> builder)
    {
        builder.ToTable("RuleWorkflows");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(e => e.Name)
            .IsUnique();

        builder.HasMany(e => e.Rules)
            .WithOne()
            .HasForeignKey(r => r.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
