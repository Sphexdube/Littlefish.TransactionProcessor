using Transaction.Infrastructure.Persistence.Context;

namespace Transaction.Presentation.Api.Dependencies;

public static class HealthCheckRegistration
{
    public static void Register(IHealthChecksBuilder builder)
    {
        builder.AddDbContextCheck<TransactionDbContext>("database");
    }
}
