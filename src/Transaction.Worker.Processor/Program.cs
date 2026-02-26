using Littlefish.TransactionProcessor.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using Transaction.Domain.Interfaces;
using Transaction.Domain.Rules;
using Transaction.Domain.Rules.Rules;
using Transaction.Infrastructure.Persistence;
using Transaction.Infrastructure.Persistence.Context;
using Transaction.Infrastructure.Persistence.Repositories;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDbContext<TransactionDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TransactionDb")));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IBatchRepository, BatchRepository>();
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<IMerchantDailySummaryRepository, MerchantDailySummaryRepository>();

builder.Services.AddScoped<IBusinessRule, NegativePurchaseAmountRule>();
builder.Services.AddScoped<IBusinessRule, RefundRequiresOriginalPurchaseRule>();
builder.Services.AddScoped<IBusinessRule, DailyMerchantLimitRule>();
builder.Services.AddScoped<IBusinessRule, HighValueReviewRule>();
builder.Services.AddScoped<IRuleEngine, RuleEngine>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
