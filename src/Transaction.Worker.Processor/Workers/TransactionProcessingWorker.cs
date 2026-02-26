using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Transaction.Application.Models.Messaging;
using Transaction.Domain.Entities;
using Transaction.Domain.Entities.Enums;
using Transaction.Domain.Interfaces;
using Transaction.Domain.Rules;

namespace Transaction.Worker.Processor.Workers;

public sealed class TransactionProcessingWorker : BackgroundService
{
    private const int MaxRetries = 3;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ILogger<TransactionProcessingWorker> _logger;
    private readonly string _queueName;

    private ServiceBusProcessor? _processor;

    public TransactionProcessingWorker(
        IServiceScopeFactory scopeFactory,
        ServiceBusClient serviceBusClient,
        ILogger<TransactionProcessingWorker> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _serviceBusClient = serviceBusClient;
        _logger = logger;
        _queueName = configuration.GetValue<string>("ServiceBus:QueueName") ?? "transactions-ingest";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TransactionProcessingWorker starting. Listening on queue '{Queue}'.", _queueName);

        ServiceBusProcessorOptions options = new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 4,
            AutoCompleteMessages = false
        };

        _processor = _serviceBusClient.CreateProcessor(_queueName, options);
        _processor.ProcessMessageAsync += OnMessageReceivedAsync;
        _processor.ProcessErrorAsync += OnErrorAsync;

        await _processor.StartProcessingAsync(stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // shutting down — expected
        }
        finally
        {
            await _processor.StopProcessingAsync();
            await _processor.DisposeAsync();
        }

        _logger.LogInformation("TransactionProcessingWorker stopped.");
    }

    private async Task OnMessageReceivedAsync(ProcessMessageEventArgs args)
    {
        TransactionMessagePayload? payload = JsonSerializer.Deserialize<TransactionMessagePayload>(
            args.Message.Body.ToString());

        if (payload == null)
        {
            _logger.LogError("Failed to deserialise message {MessageId}. Dead-lettering.", args.Message.MessageId);
            await args.DeadLetterMessageAsync(args.Message, "DeserializationFailed", "Payload could not be deserialised.");
            return;
        }

        IUnitOfWork? unitOfWork = null;

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
            unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            IRuleEngine ruleEngine = scope.ServiceProvider.GetRequiredService<IRuleEngine>();

            try
            {
                TransactionRecord? transaction = await unitOfWork.Transactions.GetByTransactionIdAsync(
                    payload.TenantId, payload.TransactionId, args.CancellationToken);

                if (transaction == null)
                {
                    _logger.LogWarning(
                        "TransactionRecord not found for TenantId={TenantId}, TransactionId={TransactionId}. Dead-lettering.",
                        payload.TenantId, payload.TransactionId);
                    await args.DeadLetterMessageAsync(args.Message, "NotFound", "TransactionRecord not found in database.");
                    return;
                }

                if (transaction.Status != TransactionStatus.Received)
                {
                    _logger.LogInformation(
                        "Transaction {TransactionId} already processed (Status={Status}). Completing message (idempotent).",
                        payload.TransactionId, transaction.Status);
                    await args.CompleteMessageAsync(args.Message);
                    return;
                }

                Tenant? tenant = await unitOfWork.Tenants.GetByIdAsync(payload.TenantId, args.CancellationToken);

                if (tenant == null)
                {
                    _logger.LogError("Tenant {TenantId} not found. Dead-lettering.", payload.TenantId);
                    await args.DeadLetterMessageAsync(args.Message, "TenantNotFound", $"Tenant {payload.TenantId} not found.");
                    return;
                }

                await unitOfWork.BeginTransactionAsync(args.CancellationToken);

                transaction.BeginProcessing();
                await unitOfWork.SaveChangesAsync(args.CancellationToken);

                DateOnly date = DateOnly.FromDateTime(transaction.OccurredAt.UtcDateTime);

                MerchantDailySummary? summary = await unitOfWork.MerchantDailySummaries.GetByMerchantAndDateAsync(
                    transaction.TenantId, transaction.MerchantId, date, args.CancellationToken);

                RuleContext ruleContext = new RuleContext(transaction, tenant, summary?.TotalAmount ?? 0m);
                IEnumerable<RuleResult> results = await ruleEngine.EvaluateAllAsync(ruleContext, args.CancellationToken);

                RuleResult? failure = results.FirstOrDefault(r => !r.IsValid);
                RuleResult? review = results.FirstOrDefault(r => r.RequiresReview);

                if (failure != null)
                {
                    transaction.Reject(failure.ErrorMessage ?? string.Empty);
                }
                else
                {
                    if (review != null)
                        transaction.MarkForReview(review.ErrorMessage);
                    else
                        transaction.Complete();

                    if (transaction.Type == TransactionType.Purchase)
                    {
                        if (summary == null)
                        {
                            MerchantDailySummary newSummary = MerchantDailySummary.Create(
                                transaction.TenantId,
                                transaction.MerchantId,
                                date,
                                transaction.Amount);
                            await unitOfWork.MerchantDailySummaries.AddAsync(newSummary, args.CancellationToken);
                        }
                        else
                        {
                            summary.AddAmount(transaction.Amount);
                        }
                    }
                }

                await unitOfWork.SaveChangesAsync(args.CancellationToken);
                await unitOfWork.CommitTransactionAsync(args.CancellationToken);

                _logger.LogInformation("Transaction {TransactionId} → {Status}",
                    transaction.TransactionId, transaction.Status);

                await args.CompleteMessageAsync(args.Message);
                return;
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxRetries)
            {
                await unitOfWork.RollbackTransactionAsync(args.CancellationToken);
                _logger.LogWarning(
                    "Concurrency conflict processing {TransactionId}, retrying (attempt {Attempt}/{Max}).",
                    payload.TransactionId, attempt, MaxRetries);
            }
            catch (DbUpdateConcurrencyException)
            {
                await unitOfWork.RollbackTransactionAsync(args.CancellationToken);
                _logger.LogError(
                    "Concurrency conflict on {TransactionId} exhausted retries. Abandoning for redelivery.",
                    payload.TransactionId);
                await args.AbandonMessageAsync(args.Message);
                return;
            }
            catch (OperationCanceledException)
            {
                if (unitOfWork != null)
                {
                    try { await unitOfWork.RollbackTransactionAsync(args.CancellationToken); } catch { /* ignore */ }
                }
                await args.AbandonMessageAsync(args.Message);
                return;
            }
            catch (Exception ex)
            {
                if (unitOfWork != null)
                {
                    try { await unitOfWork.RollbackTransactionAsync(args.CancellationToken); } catch { /* ignore */ }
                }
                _logger.LogError(ex, "Unexpected error processing transaction {TransactionId}.", payload.TransactionId);
                await args.DeadLetterMessageAsync(args.Message, "UnexpectedError", ex.Message);
                return;
            }
        }
    }

    private Task OnErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception,
            "Service Bus error. Source={ErrorSource}, Entity={EntityPath}.",
            args.ErrorSource, args.EntityPath);
        return Task.CompletedTask;
    }
}
