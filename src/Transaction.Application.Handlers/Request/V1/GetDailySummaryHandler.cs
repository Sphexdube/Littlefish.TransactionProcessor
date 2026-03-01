using Transaction.Application.Constants;
using Transaction.Application.Models.Response.V1;
using Transaction.Domain.Commands;
using Transaction.Domain.Entities;
using Transaction.Domain.Entities.Exceptions;
using Transaction.Domain.Interfaces;
using Transaction.Domain.Observability;
using Transaction.Domain.Observability.Contracts;

namespace Transaction.Application.Handlers.Request.V1;

public sealed class GetDailySummaryHandler(
    IUnitOfWork unitOfWork,
    IObservabilityManager observabilityManager) : IRequestHandler<GetDailySummaryQuery, DailySummaryResponse>
{
    public async Task<DailySummaryResponse> HandleAsync(GetDailySummaryQuery query, CancellationToken cancellationToken = default)
    {
        observabilityManager.LogMessage(InfoMessages.MethodStarted).AsInfo();

        MerchantDailySummary? summary = await unitOfWork.MerchantDailySummaries.GetByMerchantAndDateAsync(
            query.TenantId, query.MerchantId, query.Date, cancellationToken);

        if (summary == null)
            throw new NotFoundException(ErrorMessages.DailySummaryNotFound);

        observabilityManager.LogMessage(InfoMessages.MethodCompleted).AsInfo();

        return new()
        {
            MerchantId = summary.MerchantId,
            Date = summary.Date,
            TotalAmount = summary.TotalAmount,
            TransactionCount = summary.TransactionCount,
            LastCalculatedAt = summary.LastCalculatedAt
        };
    }
}
