using Transaction.Domain.Entities;
using Transaction.Tests.Unit.Models;

namespace Transaction.Tests.Unit.Builders;

public sealed class TenantBuilder() : BuilderBase<Tenant>(new Tenant())
{
    public TenantBuilder WithId(Guid id) =>
        (TenantBuilder)With(x => x.Id, id);

    public TenantBuilder WithName(string name) =>
        (TenantBuilder)With(x => x.Name, name);

    public TenantBuilder WithIsActive(bool isActive) =>
        (TenantBuilder)With(x => x.IsActive, isActive);

    public TenantBuilder WithDailyMerchantLimit(decimal limit) =>
        (TenantBuilder)With(x => x.DailyMerchantLimit, limit);

    public TenantBuilder WithHighValueThreshold(decimal threshold) =>
        (TenantBuilder)With(x => x.HighValueThreshold, threshold);

    public static Tenant BuildDefault() =>
        new TenantBuilder()
            .WithId(DefaultTestIds.TenantId)
            .WithName("Test Tenant")
            .WithIsActive(true)
            .WithDailyMerchantLimit(100_000m)
            .WithHighValueThreshold(10_000m)
            .Build();
}
