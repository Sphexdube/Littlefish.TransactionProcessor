using Transaction.Application.Constants;
using Transaction.Application.Models.Response.V1;
using Transaction.Domain.Commands;
using Transaction.Domain.Entities;
using Transaction.Domain.Entities.Exceptions;
using Transaction.Domain.Interfaces;

namespace Transaction.Application.Handlers.Request.V1;

public sealed class GetDailySummaryHandler : IRequestHandler<GetDailySummaryQuery, DailySummaryResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetDailySummaryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DailySummaryResponse> HandleAsync(GetDailySummaryQuery query, CancellationToken cancellationToken = default)
    {
        MerchantDailySummary? summary = await _unitOfWork.MerchantDailySummaries.GetByMerchantAndDateAsync(
            query.TenantId, query.MerchantId, query.Date, cancellationToken);

        if (summary == null)
            throw new NotFoundException(ErrorMessages.DailySummaryNotFound);

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
