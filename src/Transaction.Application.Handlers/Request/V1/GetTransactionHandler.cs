using System.Text.Json;
using Transaction.Application.Constants;
using Transaction.Application.Models.Response.V1;
using Transaction.Domain.Entities.Exceptions;
using Transaction.Domain.Interfaces;

namespace Transaction.Application.Handlers.Request.V1;

public sealed class GetTransactionHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetTransactionQuery, TransactionResponse>
{
    public async Task<TransactionResponse> HandleAsync(GetTransactionQuery query, CancellationToken cancellationToken = default)
    {
        var transaction = await unitOfWork.Transactions.GetByTransactionIdAsync(
            query.TenantId, query.TransactionId, cancellationToken);

        if (transaction == null)
            throw new NotFoundException(ErrorMessages.TransactionNotFound);

        return new TransactionResponse(
            transaction.Id,
            transaction.TransactionId,
            transaction.MerchantId,
            transaction.Amount,
            transaction.Currency,
            transaction.Type.ToString().ToUpperInvariant(),
            transaction.Status.ToString().ToUpperInvariant(),
            transaction.OriginalTransactionId,
            transaction.OccurredAt,
            transaction.CreatedAt,
            transaction.ProcessedAt,
            transaction.RejectionReason,
            transaction.Metadata != null
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(transaction.Metadata)
                : null);
    }
}
