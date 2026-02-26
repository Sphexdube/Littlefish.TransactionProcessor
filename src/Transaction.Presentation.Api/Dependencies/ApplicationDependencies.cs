using FluentValidation;
using Transaction.Application.Handlers;
using Transaction.Application.Handlers.Request.V1;
using Transaction.Application.Models.Response.V1;
using Transaction.Application.Validators.V1;
using Transaction.Domain.Commands;

namespace Transaction.Presentation.Api.Dependencies;

internal static class ApplicationDependencies
{
    internal static IServiceCollection AddApplicationDependencies(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<IngestTransactionBatchRequestValidator>();

        services.AddScoped<IRequestHandler<IngestBatchCommand, IngestBatchResponse>, IngestBatchHandler>();
        services.AddScoped<IRequestHandler<GetTransactionQuery, TransactionResponse>, GetTransactionHandler>();
        services.AddScoped<IRequestHandler<GetDailySummaryQuery, DailySummaryResponse>, GetDailySummaryHandler>();

        return services;
    }
}
