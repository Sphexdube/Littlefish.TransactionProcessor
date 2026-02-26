using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Transaction.Domain.Interfaces;
using Transaction.Infrastructure.Persistence.Context;
using Transaction.Infrastructure.Persistence.Repositories;

namespace Transaction.Infrastructure.Persistence.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<TransactionDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IBatchRepository, BatchRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IMerchantDailySummaryRepository, MerchantDailySummaryRepository>();

        return services;
    }
}
