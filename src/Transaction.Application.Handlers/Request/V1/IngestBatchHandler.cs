using System.Text.Json;
using Transaction.Application.Constants;
using Transaction.Application.Models.Request.V1;
using Transaction.Application.Models.Response.V1;
using Transaction.Domain.Entities;
using Transaction.Domain.Entities.Enums;
using Transaction.Domain.Entities.Exceptions;
using Transaction.Domain.Interfaces;

namespace Transaction.Application.Handlers.Request.V1;

public class IngestBatchHandler : IRequestHandler<IngestBatchCommand, IngestBatchResponse>
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

        var errors = new List<TransactionValidationError>();
        int acceptedCount = 0;
        int rejectedCount = 0;

        var batch = new Batch
        {
            Id = Guid.NewGuid(),
            TenantId = command.TenantId,
            Status = BatchStatus.Received,
            TotalCount = command.Request.Transactions.Count,
            CorrelationId = command.CorrelationId
        };

        await _unitOfWork.Batches.AddAsync(batch, cancellationToken);

        foreach (var item in command.Request.Transactions)
        {
            if (await _unitOfWork.Transactions.ExistsByTransactionIdAsync(command.TenantId, item.TransactionId, cancellationToken))
            {
                errors.Add(new TransactionValidationError(item.TransactionId, "Duplicate transaction ID"));
                rejectedCount++;
                continue;
            }

            var transactionType = Enum.Parse<TransactionType>(item.Type, ignoreCase: true);

            var transaction = new TransactionRecord
            {
                Id = Guid.NewGuid(),
                TenantId = command.TenantId,
                BatchId = batch.Id,
                TransactionId = item.TransactionId,
                MerchantId = item.MerchantId,
                Amount = item.Amount,
                Currency = item.Currency.ToUpperInvariant(),
                Type = transactionType,
                OriginalTransactionId = item.OriginalTransactionId,
                OccurredAt = item.OccurredAt,
                Status = TransactionStatus.Received,
                Metadata = item.Metadata != null
                    ? JsonSerializer.Serialize(item.Metadata)
                    : null
            };

            await _unitOfWork.Transactions.AddAsync(transaction, cancellationToken);
            acceptedCount++;
        }

        batch.AcceptedCount = acceptedCount;
        batch.RejectedCount = rejectedCount;
        batch.QueuedCount = acceptedCount;
        batch.Status = BatchStatus.Processing;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new IngestBatchResponse(
            batch.Id,
            acceptedCount,
            rejectedCount,
            acceptedCount,
            command.CorrelationId,
            errors.Count > 0 ? errors : null);
    }
}
