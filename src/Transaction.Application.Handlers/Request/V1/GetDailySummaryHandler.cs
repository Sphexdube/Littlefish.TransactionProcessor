using Transaction.Application.Constants;
using Transaction.Application.Models.Response.V1;
using Transaction.Domain.Entities.Exceptions;
using Transaction.Domain.Interfaces;

namespace Transaction.Application.Handlers.Request.V1;

public sealed class GetDailySummaryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetDailySummaryQuery, DailySummaryResponse>
{
    public async Task<DailySummaryResponse> HandleAsync(GetDailySummaryQuery query, CancellationToken cancellationToken = default)
    {
        var summary = await unitOfWork.MerchantDailySummaries.GetByMerchantAndDateAsync(
            query.TenantId, query.MerchantId, query.Date, cancellationToken);

        if (summary == null)
            throw new NotFoundException(ErrorMessages.DailySummaryNotFound);

        return new DailySummaryResponse(
            summary.MerchantId,
            summary.Date,
            summary.TotalAmount,
            summary.TransactionCount,
            summary.LastCalculatedAt);
    }
}
