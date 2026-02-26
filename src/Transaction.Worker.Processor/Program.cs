using Littlefish.TransactionProcessor.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Transaction.Domain.Interfaces;
using Transaction.Domain.Rules;
using Transaction.Domain.Rules.Rules;
using Transaction.Infrastructure.Persistence;
using Transaction.Infrastructure.Persistence.Context;
using Transaction.Infrastructure.Persistence.Repositories;
using Transaction.Worker.Processor.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDbContext<TransactionDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TransactionDb")));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<TransactionDbContext>("database");

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

builder.Services.AddHostedService<TransactionProcessingWorker>();

var host = builder.Build();
host.Run();
