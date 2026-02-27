using Transaction.Application.Constants;
using Transaction.Application.Models.Response.V1;
using Transaction.Domain.Commands;
using Transaction.Domain.Entities;
using Transaction.Domain.Entities.Exceptions;
using Transaction.Domain.Interfaces;
using Transaction.Domain.Observability;
using Transaction.Domain.Observability.Contracts;

namespace Transaction.Application.Handlers.Request.V1;

public sealed class GetDailySummaryHandler : IRequestHandler<GetDailySummaryQuery, DailySummaryResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IObservabilityManager _observabilityManager;

    public GetDailySummaryHandler(IUnitOfWork unitOfWork, IObservabilityManager observabilityManager)
    {
        _unitOfWork = unitOfWork;
        _observabilityManager = observabilityManager;
    }

    public async Task<DailySummaryResponse> HandleAsync(GetDailySummaryQuery query, CancellationToken cancellationToken = default)
    {
        _observabilityManager.LogMessage(InfoMessages.MethodStarted).AsInfo();

        MerchantDailySummary? summary = await _unitOfWork.MerchantDailySummaries.GetByMerchantAndDateAsync(
            query.TenantId, query.MerchantId, query.Date, cancellationToken);

        if (summary == null)
            throw new NotFoundException(ErrorMessages.DailySummaryNotFound);

        _observabilityManager.LogMessage(InfoMessages.MethodCompleted).AsInfo();

        return new DailySummaryResponse
        {
            MerchantId = summary.MerchantId,
            Date = summary.Date,
            TotalAmount = summary.TotalAmount,
            TransactionCount = summary.TransactionCount,
            LastCalculatedAt = summary.LastCalculatedAt
        };
    }
}
