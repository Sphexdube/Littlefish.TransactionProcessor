using Microsoft.Extensions.DependencyInjection;
using Transaction.Domain.Entities;
using Transaction.Infrastructure.Persistence.Context;
using Transaction.Tests.Unit.Builders;
using Transaction.Tests.Unit.Models;

namespace Transaction.Tests.Unit.Setup;

public static class DefaultDataSetup
{
    public static async Task AddDefaultTenant(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        TransactionDbContext context = serviceProvider.GetRequiredService<TransactionDbContext>();

        Tenant tenant = TenantBuilder.BuildDefault();

        await context.Tenants.AddAsync(tenant, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public static async Task AddTenant(IServiceProvider serviceProvider, Guid tenantId, string name = "Test Tenant", CancellationToken cancellationToken = default)
    {
        TransactionDbContext context = serviceProvider.GetRequiredService<TransactionDbContext>();

        Tenant tenant = new TenantBuilder()
            .WithId(tenantId)
            .WithName(name)
            .WithIsActive(true)
            .WithDailyMerchantLimit(100_000m)
            .WithHighValueThreshold(10_000m)
            .Build();

        await context.Tenants.AddAsync(tenant, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public static async Task AddMerchantDailySummary(
        IServiceProvider serviceProvider,
        string merchantId,
        DateOnly date,
        decimal totalAmount,
        int transactionCount,
        CancellationToken cancellationToken = default)
    {
        TransactionDbContext context = serviceProvider.GetRequiredService<TransactionDbContext>();

        MerchantDailySummary summary = MerchantDailySummary.Create(
            DefaultTestIds.TenantId,
            merchantId,
            date,
            totalAmount);

        for (int i = 1; i < transactionCount; i++)
        {
            summary.AddAmount(totalAmount / transactionCount);
        }

        await context.MerchantDailySummaries.AddAsync(summary, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
