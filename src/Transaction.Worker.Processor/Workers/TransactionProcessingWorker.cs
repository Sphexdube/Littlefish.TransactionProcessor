using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Transaction.Application.Models.Messaging;
using Transaction.Domain.Entities;
using Transaction.Domain.Entities.Enums;
using Transaction.Domain.Interfaces;
using Transaction.Domain.Observability;
using Transaction.Domain.Observability.Contracts;
using Transaction.Domain.Rules;

namespace Transaction.Worker.Processor.Workers;

public sealed class TransactionProcessingWorker : BackgroundService
{
    private const int MaxRetries = 3;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly IObservabilityManager _observabilityManager;
    private readonly string _queueName;

    private ServiceBusProcessor? _processor;

    public TransactionProcessingWorker(
        IServiceScopeFactory scopeFactory,
        ServiceBusClient serviceBusClient,
        IObservabilityManager observabilityManager,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _serviceBusClient = serviceBusClient;
        _observabilityManager = observabilityManager;
        _queueName = configuration.GetValue<string>("ServiceBus:QueueName") ?? "transactions-ingest";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _observabilityManager.LogMessage(InfoMessages.MethodStarted).AsInfo();

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

        _observabilityManager.LogMessage(InfoMessages.MethodCompleted).AsInfo();
    }

    private async Task OnMessageReceivedAsync(ProcessMessageEventArgs args)
    {
        _observabilityManager.LogMessage(InfoMessages.MethodStarted).AsInfo();

        TransactionMessagePayload? payload = JsonSerializer.Deserialize<TransactionMessagePayload>(
            args.Message.Body.ToString());

        if (payload == null)
        {
            _observabilityManager.LogMessage($"Failed to deserialise message {args.Message.MessageId}. Dead-lettering.").AsError();
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
                    _observabilityManager.LogMessage($"TransactionRecord not found for TenantId={payload.TenantId}, TransactionId={payload.TransactionId}. Dead-lettering.").AsWarning();
                    await args.DeadLetterMessageAsync(args.Message, "NotFound", "TransactionRecord not found in database.");
                    return;
                }

                if (transaction.Status != TransactionStatus.Received)
                {
                    _observabilityManager.LogMessage($"Transaction {payload.TransactionId} already processed (Status={transaction.Status}). Completing message (idempotent).").AsInfo();
                    await args.CompleteMessageAsync(args.Message);
                    return;
                }

                Tenant? tenant = await unitOfWork.Tenants.GetByIdAsync(payload.TenantId, args.CancellationToken);

                if (tenant == null)
                {
                    _observabilityManager.LogMessage($"Tenant {payload.TenantId} not found. Dead-lettering.").AsError();
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

                _observabilityManager.LogMessage($"Transaction {transaction.TransactionId} processed with status {transaction.Status}.").AsInfo();

                await args.CompleteMessageAsync(args.Message);
                return;
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxRetries)
            {
                await unitOfWork.RollbackTransactionAsync(args.CancellationToken);
                _observabilityManager.LogMessage($"Concurrency conflict processing {payload.TransactionId}, retrying (attempt {attempt}/{MaxRetries}).").AsWarning();
            }
            catch (DbUpdateConcurrencyException)
            {
                await unitOfWork.RollbackTransactionAsync(args.CancellationToken);
                _observabilityManager.LogMessage($"Concurrency conflict on {payload.TransactionId} exhausted retries. Abandoning for redelivery.").AsError();
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
                _observabilityManager.LogMessage($"Unexpected error processing transaction {payload.TransactionId}: {ex.Message}").AsError();
                await args.DeadLetterMessageAsync(args.Message, "UnexpectedError", ex.Message);
                return;
            }
        }
    }

    private Task OnErrorAsync(ProcessErrorEventArgs args)
    {
        _observabilityManager.LogMessage($"Service Bus error. Source={args.ErrorSource}, Entity={args.EntityPath}: {args.Exception.Message}").AsError();
        return Task.CompletedTask;
    }
}
