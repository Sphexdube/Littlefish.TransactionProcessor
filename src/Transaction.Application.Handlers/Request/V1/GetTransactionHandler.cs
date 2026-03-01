using System.Text.Json;
using Transaction.Application.Constants;
using Transaction.Application.Models.Response.V1;
using Transaction.Domain.Commands;
using Transaction.Domain.Entities;
using Transaction.Domain.Entities.Exceptions;
using Transaction.Domain.Interfaces;
using Transaction.Domain.Observability;
using Transaction.Domain.Observability.Contracts;

namespace Transaction.Application.Handlers.Request.V1;

public sealed class GetTransactionHandler(
    IUnitOfWork unitOfWork,
    IObservabilityManager observabilityManager) : IRequestHandler<GetTransactionQuery, TransactionResponse>
{
    public async Task<TransactionResponse> HandleAsync(GetTransactionQuery query, CancellationToken cancellationToken = default)
    {
        observabilityManager.LogMessage(InfoMessages.MethodStarted).AsInfo();

        TransactionRecord? transaction = await unitOfWork.Transactions.GetByTransactionIdAsync(
            query.TenantId, query.TransactionId, cancellationToken);

        if (transaction == null)
            throw new NotFoundException(ErrorMessages.TransactionNotFound);

        observabilityManager.LogMessage(InfoMessages.MethodCompleted).AsInfo();

        return new()
        {
            Id = transaction.Id,
            TransactionId = transaction.TransactionId,
            MerchantId = transaction.MerchantId,
            Amount = transaction.Amount,
            Currency = transaction.Currency,
            Type = transaction.Type.ToString().ToUpperInvariant(),
            Status = transaction.Status.ToString().ToUpperInvariant(),
            OriginalTransactionId = transaction.OriginalTransactionId,
            OccurredAt = transaction.OccurredAt,
            CreatedAt = transaction.CreatedAt,
            ProcessedAt = transaction.ProcessedAt,
            RejectionReason = transaction.RejectionReason,
            Metadata = transaction.Metadata != null
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(transaction.Metadata)
                : null
        };
    }
}
