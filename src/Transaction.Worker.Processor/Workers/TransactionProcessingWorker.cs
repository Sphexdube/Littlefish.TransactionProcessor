using System.Diagnostics;
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
using Transaction.Worker.Processor;

namespace Transaction.Worker.Processor.Workers;

public sealed class TransactionProcessingWorker(
    IServiceScopeFactory scopeFactory,
    ServiceBusClient serviceBusClient,
    IObservabilityManager observabilityManager,
    IMetricRecorder metricRecorder,
    IConfiguration configuration) : BackgroundService
{
    private const int MaxRetries = 3;

    private readonly string _queueName = configuration.GetValue<string>("ServiceBus:QueueName") ?? "transactions-ingest";

    private ServiceBusProcessor? _processor;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        observabilityManager.LogMessage(InfoMessages.MethodStarted).AsInfo();

        ServiceBusProcessorOptions options = new()
        {
            MaxConcurrentCalls = 4,
            AutoCompleteMessages = false
        };

        _processor = serviceBusClient.CreateProcessor(_queueName, options);
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

        observabilityManager.LogMessage(InfoMessages.MethodCompleted).AsInfo();
    }

    private async Task OnMessageReceivedAsync(ProcessMessageEventArgs args)
    {
        observabilityManager.LogMessage(InfoMessages.MethodStarted).AsInfo();

        Stopwatch stopwatch = Stopwatch.StartNew();

        TransactionMessagePayload? payload = JsonSerializer.Deserialize<TransactionMessagePayload>(
            args.Message.Body.ToString());

        if (payload == null)
        {
            metricRecorder.Increment(MetricDefinitions.ServiceBusDeadLettered);
            observabilityManager.LogMessage(string.Format(LogMessages.FailedToDeserialiseMessage, args.Message.MessageId)).AsError();
            await args.DeadLetterMessageAsync(args.Message, ServiceBusReasons.DeserializationFailed, ServiceBusReasons.DeserializationFailedDescription);
            return;
        }

        IUnitOfWork? unitOfWork = null;

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            IRuleEngine ruleEngine = scope.ServiceProvider.GetRequiredService<IRuleEngine>();

            try
            {
                TransactionRecord? transaction = await unitOfWork.Transactions.GetByTransactionIdAsync(
                    payload.TenantId, payload.TransactionId, args.CancellationToken);

                if (transaction == null)
                {
                    metricRecorder.Increment(MetricDefinitions.ServiceBusDeadLettered);
                    observabilityManager.LogMessage(string.Format(LogMessages.TransactionNotFoundDeadLetter, payload.TenantId, payload.TransactionId)).AsWarning();
                    await args.DeadLetterMessageAsync(args.Message, ServiceBusReasons.NotFound, ServiceBusReasons.NotFoundDescription);
                    return;
                }

                if (transaction.Status != TransactionStatus.Received)
                {
                    observabilityManager.LogMessage(string.Format(LogMessages.TransactionAlreadyProcessed, payload.TransactionId, transaction.Status)).AsInfo();
                    await args.CompleteMessageAsync(args.Message);
                    return;
                }

                Tenant? tenant = await unitOfWork.Tenants.GetByIdAsync(payload.TenantId, args.CancellationToken);

                if (tenant == null)
                {
                    metricRecorder.Increment(MetricDefinitions.ServiceBusDeadLettered);
                    observabilityManager.LogMessage(string.Format(LogMessages.TenantNotFoundDeadLetter, payload.TenantId)).AsError();
                    await args.DeadLetterMessageAsync(args.Message, ServiceBusReasons.TenantNotFound, string.Format(ServiceBusReasons.TenantNotFoundDescriptionFormat, payload.TenantId));
                    return;
                }

                await unitOfWork.BeginTransactionAsync(args.CancellationToken);

                transaction.BeginProcessing();
                await unitOfWork.SaveChangesAsync(args.CancellationToken);

                DateOnly date = DateOnly.FromDateTime(transaction.OccurredAt.UtcDateTime);

                MerchantDailySummary? summary = await unitOfWork.MerchantDailySummaries.GetByMerchantAndDateAsync(
                    transaction.TenantId, transaction.MerchantId, date, args.CancellationToken);

                if (transaction.Type == TransactionType.Purchase)
                    metricRecorder.Increment(MetricDefinitions.DailyLimitChecks);

                RuleContext ruleContext = new(transaction, tenant, summary?.TotalAmount ?? 0m);
                IEnumerable<RuleResult> results = await ruleEngine.EvaluateAllAsync(ruleContext, args.CancellationToken);

                RuleResult? failure = results.FirstOrDefault(r => !r.IsValid);
                RuleResult? review = results.FirstOrDefault(r => r.RequiresReview);

                if (failure != null)
                {
                    if (failure.ErrorMessage == RuleMessages.DailyMerchantLimitError)
                        metricRecorder.Increment(MetricDefinitions.DailyLimitExceeded);

                    transaction.Reject(failure.ErrorMessage ?? string.Empty);
                    metricRecorder.Increment(MetricDefinitions.TransactionsRejected);
                }
                else
                {
                    if (review != null)
                    {
                        transaction.MarkForReview(review.ErrorMessage);
                        metricRecorder.Increment(MetricDefinitions.TransactionsInReview);
                    }
                    else
                    {
                        transaction.Complete();
                        metricRecorder.Increment(MetricDefinitions.TransactionsProcessed);
                    }

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

                stopwatch.Stop();
                metricRecorder.RecordDuration(MetricDefinitions.ProcessingDuration, stopwatch.ElapsedMilliseconds);

                observabilityManager.LogMessage(string.Format(LogMessages.TransactionProcessed, transaction.TransactionId, transaction.Status)).AsInfo();

                await args.CompleteMessageAsync(args.Message);
                return;
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxRetries)
            {
                await unitOfWork.RollbackTransactionAsync(args.CancellationToken);
                metricRecorder.Increment(MetricDefinitions.ConcurrencyConflicts);
                observabilityManager.LogMessage(string.Format(LogMessages.ConcurrencyConflictRetry, payload.TransactionId, attempt, MaxRetries)).AsWarning();
            }
            catch (DbUpdateConcurrencyException)
            {
                await unitOfWork.RollbackTransactionAsync(args.CancellationToken);
                metricRecorder.Increment(MetricDefinitions.ConcurrencyConflicts);
                metricRecorder.Increment(MetricDefinitions.ServiceBusAbandoned);
                observabilityManager.LogMessage(string.Format(LogMessages.ConcurrencyConflictExhausted, payload.TransactionId)).AsError();
                await args.AbandonMessageAsync(args.Message);
                return;
            }
            catch (OperationCanceledException)
            {
                if (unitOfWork != null)
                {
                    try { await unitOfWork.RollbackTransactionAsync(args.CancellationToken); } catch { /* ignore */ }
                }
                metricRecorder.Increment(MetricDefinitions.ServiceBusAbandoned);
                await args.AbandonMessageAsync(args.Message);
                return;
            }
            catch (Exception ex)
            {
                if (unitOfWork != null)
                {
                    try { await unitOfWork.RollbackTransactionAsync(args.CancellationToken); } catch { /* ignore */ }
                }
                metricRecorder.Increment(MetricDefinitions.ServiceBusDeadLettered);
                observabilityManager.LogMessage(string.Format(LogMessages.UnexpectedErrorProcessingTransaction, payload.TransactionId, ex.Message)).AsError();
                await args.DeadLetterMessageAsync(args.Message, ServiceBusReasons.UnexpectedError, ex.Message);
                return;
            }
        }
    }

    private Task OnErrorAsync(ProcessErrorEventArgs args)
    {
        observabilityManager.LogMessage(string.Format(LogMessages.ServiceBusError, args.ErrorSource, args.EntityPath, args.Exception.Message)).AsError();
        return Task.CompletedTask;
    }
}
