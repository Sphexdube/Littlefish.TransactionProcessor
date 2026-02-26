using Asp.Versioning;
using FluentValidation;
using Littlefish.TransactionProcessor.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using Transaction.Application.Handlers;
using Transaction.Application.Handlers.Request.V1;
using Transaction.Application.Models.Response.V1;
using Transaction.Presentation.Api.Middleware;
using Transaction.Application.Validators.V1;
using Transaction.Domain.Interfaces;
using Transaction.Domain.Rules;
using Transaction.Domain.Rules.Rules;
using Transaction.Infrastructure.Persistence;
using Transaction.Infrastructure.Persistence.Context;
using Transaction.Infrastructure.Persistence.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddMvc();

builder.Services.AddDbContext<TransactionDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TransactionDb")));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IBatchRepository, BatchRepository>();
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<IMerchantDailySummaryRepository, MerchantDailySummaryRepository>();

builder.Services.AddValidatorsFromAssemblyContaining<IngestTransactionBatchRequestValidator>();

builder.Services.AddScoped<IRequestHandler<IngestBatchCommand, IngestBatchResponse>, IngestBatchHandler>();
builder.Services.AddScoped<IRequestHandler<GetTransactionQuery, TransactionResponse>, GetTransactionHandler>();
builder.Services.AddScoped<IRequestHandler<GetDailySummaryQuery, DailySummaryResponse>, GetDailySummaryHandler>();

builder.Services.AddScoped<IBusinessRule, NegativePurchaseAmountRule>();
builder.Services.AddScoped<IBusinessRule, RefundRequiresOriginalPurchaseRule>();
builder.Services.AddScoped<IBusinessRule, DailyMerchantLimitRule>();
builder.Services.AddScoped<IBusinessRule, HighValueReviewRule>();
builder.Services.AddScoped<IRuleEngine, RuleEngine>();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();
