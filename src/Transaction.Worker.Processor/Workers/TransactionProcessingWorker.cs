using Microsoft.EntityFrameworkCore;
using Transaction.Domain.Entities;
using Transaction.Domain.Entities.Enums;
using Transaction.Domain.Interfaces;
using Transaction.Domain.Rules;

namespace Transaction.Worker.Processor.Workers;

public sealed class TransactionProcessingWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<TransactionProcessingWorker> logger) : BackgroundService
{
    private const int BatchSize = 50;
    private const int PollingIntervalSeconds = 5;
    private const int MaxRetries = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Transaction processing worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingTransactionsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error in processing loop");
            }

            await Task.Delay(TimeSpan.FromSeconds(PollingIntervalSeconds), stoppingToken);
        }

        logger.LogInformation("Transaction processing worker stopped");
    }

    private async Task ProcessPendingTransactionsAsync(CancellationToken cancellationToken)
    {
        List<Guid> pendingIds;

        using (var scope = scopeFactory.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var pending = await uow.Transactions.GetPendingTransactionsAsync(BatchSize, cancellationToken);
            pendingIds = pending.Select(t => t.Id).ToList();
        }

        if (pendingIds.Count == 0)
            return;

        logger.LogInformation("Picked up {Count} pending transactions for processing", pendingIds.Count);

        foreach (var id in pendingIds)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await ProcessTransactionWithRetriesAsync(id, cancellationToken);
        }
    }

    private async Task ProcessTransactionWithRetriesAsync(Guid transactionRecordId, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            using var scope = scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var ruleEngine = scope.ServiceProvider.GetRequiredService<IRuleEngine>();

            try
            {
                var transaction = await uow.Transactions.GetByIdWithDetailsAsync(transactionRecordId, cancellationToken);

                if (transaction is null || transaction.Status != TransactionStatus.Received)
                    return;

                using var logScope = logger.BeginScope(new Dictionary<string, object>
                {
                    ["CorrelationId"] = transaction.Batch?.CorrelationId ?? string.Empty,
                    ["TransactionId"] = transaction.TransactionId,
                    ["TenantId"] = transaction.TenantId
                });

                await uow.BeginTransactionAsync(cancellationToken);

                transaction.Status = TransactionStatus.Processing;
                await uow.SaveChangesAsync(cancellationToken);

                var date = DateOnly.FromDateTime(transaction.OccurredAt.UtcDateTime);
                var summary = await uow.MerchantDailySummaries.GetByMerchantAndDateAsync(
                    transaction.TenantId, transaction.MerchantId, date, cancellationToken);

                var ruleContext = new RuleContext(transaction, transaction.Tenant, summary?.TotalAmount ?? 0m);
                var results = await ruleEngine.EvaluateAllAsync(ruleContext, cancellationToken);

                var failure = results.FirstOrDefault(r => !r.IsValid);
                var review = results.FirstOrDefault(r => r.RequiresReview);

                transaction.ProcessedAt = DateTimeOffset.UtcNow;

                if (failure is not null)
                {
                    transaction.Status = TransactionStatus.Rejected;
                    transaction.RejectionReason = failure.ErrorMessage;
                }
                else
                {
                    transaction.Status = review is not null ? TransactionStatus.Review : TransactionStatus.Processed;
                    transaction.RejectionReason = review?.ErrorMessage;

                    if (transaction.Type == TransactionType.Purchase)
                    {
                        if (summary is null)
                        {
                            await uow.MerchantDailySummaries.AddAsync(new MerchantDailySummary
                            {
                                Id = Guid.NewGuid(),
                                TenantId = transaction.TenantId,
                                MerchantId = transaction.MerchantId,
                                Date = date,
                                TotalAmount = transaction.Amount,
                                TransactionCount = 1,
                                LastCalculatedAt = DateTimeOffset.UtcNow
                            }, cancellationToken);
                        }
                        else
                        {
                            summary.TotalAmount += transaction.Amount;
                            summary.TransactionCount++;
                            summary.LastCalculatedAt = DateTimeOffset.UtcNow;
                        }
                    }
                }

                await uow.SaveChangesAsync(cancellationToken); // RowVersion check on MerchantDailySummary
                await uow.CommitTransactionAsync(cancellationToken);

                logger.LogInformation("Transaction {TransactionId} → {Status}",
                    transaction.TransactionId, transaction.Status);
                return;
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxRetries)
            {
                await uow.RollbackTransactionAsync(cancellationToken);
                logger.LogWarning(
                    "Concurrency conflict on transaction record {Id}, retrying (attempt {Attempt}/{Max})",
                    transactionRecordId, attempt, MaxRetries);
            }
            catch (DbUpdateConcurrencyException)
            {
                await uow.RollbackTransactionAsync(cancellationToken);
                logger.LogError(
                    "Concurrency conflict on transaction record {Id} exhausted retries — will retry on next poll cycle",
                    transactionRecordId);
                await ResetToReceivedAsync(transactionRecordId, cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                try { await uow.RollbackTransactionAsync(cancellationToken); } catch { /* ignore */ }
                logger.LogError(ex, "Error processing transaction record {Id}", transactionRecordId);
                await RejectWithErrorAsync(transactionRecordId, ex.Message, cancellationToken);
                return;
            }
        }
    }

    private async Task ResetToReceivedAsync(Guid transactionRecordId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var tx = await uow.Transactions.GetByIdWithDetailsAsync(transactionRecordId, cancellationToken);
        if (tx is { Status: TransactionStatus.Processing })
        {
            tx.Status = TransactionStatus.Received;
            try { await uow.SaveChangesAsync(cancellationToken); } catch { /* ignore */ }
        }
    }

    private async Task RejectWithErrorAsync(Guid transactionRecordId, string errorMessage, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var tx = await uow.Transactions.GetByIdWithDetailsAsync(transactionRecordId, cancellationToken);
        if (tx is not null)
        {
            tx.Status = TransactionStatus.Rejected;
            tx.RejectionReason = $"Processing error: {errorMessage}";
            tx.ProcessedAt = DateTimeOffset.UtcNow;
            try { await uow.SaveChangesAsync(cancellationToken); } catch { /* ignore */ }
        }
    }
}
