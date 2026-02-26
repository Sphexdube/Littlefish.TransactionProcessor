using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transaction.Domain.Entities;

namespace Transaction.Infrastructure.Persistence.Mappings;

internal sealed class BusinessRuleMapping : IEntityTypeConfiguration<BusinessRule>
{
    public void Configure(EntityTypeBuilder<BusinessRule> builder)
    {
        builder.ToTable("Rules");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.RuleName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.RuleExpressionType)
            .HasMaxLength(50)
            .IsRequired()
            .HasDefaultValue("LambdaExpression");

        builder.Property(e => e.Expression)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(500);

        builder.Property(e => e.SuccessEvent)
            .HasMaxLength(100);

        builder.HasIndex(e => new { e.WorkflowId, e.IsActive });

        builder.HasIndex(e => new { e.WorkflowId, e.RuleName })
            .IsUnique();
    }
}
