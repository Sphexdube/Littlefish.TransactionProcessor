using Microsoft.EntityFrameworkCore;
using Transaction.Domain.Interfaces;
using Transaction.Infrastructure.Persistence;
using Transaction.Infrastructure.Persistence.Context;
using Transaction.Infrastructure.Persistence.Repositories;

namespace Transaction.Worker.Processor.Dependencies;

internal static class DatabaseDependencies
{
    internal static IServiceCollection AddDatabaseDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TransactionDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("TransactionDb")));

        services.AddHealthChecks()
            .AddDbContextCheck<TransactionDbContext>("database", tags: ["ready"]);

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IBatchRepository, BatchRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IMerchantDailySummaryRepository, MerchantDailySummaryRepository>();
        services.AddScoped<IRuleWorkflowRepository, RuleWorkflowRepository>();

        return services;
    }
}
