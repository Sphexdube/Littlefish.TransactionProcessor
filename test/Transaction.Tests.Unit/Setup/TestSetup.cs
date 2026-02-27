using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Transaction.Application.Handlers;
using Transaction.Application.Handlers.Request.V1;
using Transaction.Application.Models.Response.V1;
using Transaction.Application.Validators.V1;
using Transaction.Domain.Commands;
using Transaction.Domain.Interfaces;
using Transaction.Domain.Observability;
using Transaction.Domain.Observability.Contracts;
using Transaction.Infrastructure.Persistence;
using Transaction.Infrastructure.Persistence.Context;
using Transaction.Infrastructure.Persistence.Repositories;

namespace Transaction.Tests.Unit.Setup;

internal static class TestSetup
{
    internal static ServiceProvider CreateServiceProvider()
    {
        ServiceCollection services = new ServiceCollection();

        services.AddDbContext<TransactionDbContext>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

        services.AddLogging();

        services.AddSingleton<IObservabilityManager>(sp =>
            new ObservabilityManager(sp.GetRequiredService<ILogger<ObservabilityManager>>()));

        services.AddSingleton<IMetricRecorder>(Substitute.For<IMetricRecorder>());

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IBatchRepository, BatchRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IMerchantDailySummaryRepository, MerchantDailySummaryRepository>();
        services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();

        services.AddScoped<IRequestHandler<IngestBatchCommand, IngestBatchResponse>, IngestBatchHandler>();
        services.AddScoped<IRequestHandler<GetTransactionQuery, TransactionResponse>, GetTransactionHandler>();
        services.AddScoped<IRequestHandler<GetDailySummaryQuery, DailySummaryResponse>, GetDailySummaryHandler>();

        services.AddValidatorsFromAssemblyContaining<IngestTransactionBatchRequestValidator>();

        return services.BuildServiceProvider(validateScopes: false);
    }

    internal static WebApplicationFactory<Program> CreateWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");

        // Generate once here — captured in the closure below so every scope within
        // the same factory sees the same InMemory database and shares data.
        // If generated inside the lambda, Guid.NewGuid() would be called per-scope,
        // producing a different DB name each time → data seeded in SetUp becomes invisible
        // to the request-scope DbContext.
        string testDatabaseName = $"TestDb_{Guid.NewGuid()}";

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(host =>
            {
                host.UseEnvironment("Test");

                host.ConfigureServices(services =>
                {
                    // EF Core 7+ stores the optionsAction in IDbContextOptionsConfiguration<T>.
                    // If only DbContextOptions<T> is removed, the original SQL Server config descriptor
                    // remains and both providers (SqlServer + InMemory) get registered → exception.
                    // Must remove all IDbContextOptionsConfiguration<TransactionDbContext> descriptors too.
                    ServiceDescriptor[] efConfigDescriptors = services
                        .Where(d =>
                            d.ServiceType.IsGenericType &&
                            d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration") &&
                            d.ServiceType.GenericTypeArguments.Length == 1 &&
                            d.ServiceType.GenericTypeArguments[0] == typeof(TransactionDbContext))
                        .ToArray();

                    foreach (ServiceDescriptor efDescriptor in efConfigDescriptors)
                    {
                        services.Remove(efDescriptor);
                    }

                    ServiceDescriptor? dbDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<TransactionDbContext>));

                    if (dbDescriptor != null)
                    {
                        services.Remove(dbDescriptor);
                    }

                    // Replace SQL Server with InMemory for isolation
                    services.AddDbContext<TransactionDbContext>(options =>
                        options.UseInMemoryDatabase(testDatabaseName));

                    // Substitute Service Bus to prevent real ASB calls in integration-like tests
                    // (Workers inject IServiceBusPublisher; mock it here if worker tests are added)
                    // services.AddSingleton(Substitute.For<IServiceBusPublisher>());
                });
            });
    }

    // Returns a mock IUnitOfWork via NSubstitute for pure unit tests
    // that should not touch the database at all.
    internal static IUnitOfWork CreateMockUnitOfWork()
    {
        return Substitute.For<IUnitOfWork>();
    }
}
