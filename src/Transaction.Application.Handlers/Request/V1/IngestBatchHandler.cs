using System.Text.Json;
using Transaction.Application.Constants;
using Transaction.Application.Models.Messaging;
using Transaction.Application.Models.Response.V1;
using Transaction.Domain.Commands;
using Transaction.Domain.Entities;
using Transaction.Domain.Entities.Enums;
using Transaction.Domain.Entities.Exceptions;
using Transaction.Domain.Interfaces;

namespace Transaction.Application.Handlers.Request.V1;

public sealed class IngestBatchHandler : IRequestHandler<IngestBatchCommand, IngestBatchResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public IngestBatchHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IngestBatchResponse> HandleAsync(IngestBatchCommand command, CancellationToken cancellationToken = default)
    {
        if (await _unitOfWork.Tenants.GetByIdAsync(command.TenantId, cancellationToken) == null)
            throw new NotFoundException(ErrorMessages.TenantNotFound);

        List<TransactionValidationError> errors = new List<TransactionValidationError>();
        int acceptedCount = 0;
        int rejectedCount = 0;

        Batch batch = Batch.Create(command.TenantId, command.Transactions.Count, command.CorrelationId);

        await _unitOfWork.Batches.AddAsync(batch, cancellationToken);

        foreach (TransactionItemCommand item in command.Transactions)
        {
            if (await _unitOfWork.Transactions.ExistsByTransactionIdAsync(command.TenantId, item.TransactionId, cancellationToken))
            {
                errors.Add(new TransactionValidationError
                {
                    TransactionId = item.TransactionId,
                    ErrorMessage = "Duplicate transaction ID"
                });
                rejectedCount++;
                continue;
            }

            TransactionType transactionType = Enum.Parse<TransactionType>(item.Type, ignoreCase: true);

            TransactionRecord transaction = TransactionRecord.Create(
                command.TenantId,
                batch.Id,
                item.TransactionId,
                item.MerchantId,
                item.Amount,
                item.Currency.ToUpperInvariant(),
                transactionType,
                item.OriginalTransactionId,
                item.OccurredAt,
                item.Metadata != null ? JsonSerializer.Serialize(item.Metadata) : null);

            await _unitOfWork.Transactions.AddAsync(transaction, cancellationToken);

            TransactionMessagePayload payload = new TransactionMessagePayload
            {
                TenantId = command.TenantId,
                TransactionId = item.TransactionId,
                MerchantId = item.MerchantId,
                Amount = item.Amount,
                Currency = item.Currency.ToUpperInvariant(),
                Type = item.Type,
                OriginalTransactionId = item.OriginalTransactionId,
                OccurredAt = item.OccurredAt,
                Metadata = item.Metadata,
                BatchId = batch.Id,
                CorrelationId = command.CorrelationId
            };

            OutboxMessage outboxMessage = OutboxMessage.Create(
                Guid.NewGuid(),
                command.TenantId,
                item.TransactionId,
                JsonSerializer.Serialize(payload));

            await _unitOfWork.OutboxMessages.AddAsync(outboxMessage, cancellationToken);

            acceptedCount++;
        }

        batch.UpdateCounts(acceptedCount, rejectedCount, acceptedCount);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new IngestBatchResponse
        {
            BatchId = batch.Id,
            AcceptedCount = acceptedCount,
            RejectedCount = rejectedCount,
            QueuedCount = acceptedCount,
            CorrelationId = command.CorrelationId,
            Errors = errors.Count > 0 ? errors : null
        };
    }
}
